using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Integrations;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

public sealed class LogisticsFiscalDispatcher(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<LogisticsFiscalDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LogisticsFiscalDispatcher encountered an unexpected error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var activeTenants = await catalogClient.ListAllAsync(cancellationToken);

        foreach (var tenant in activeTenants.Where(t => t.IsActive))
        {
            try
            {
                await DispatchTenantBatchAsync(tenant, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("was not found in configuration"))
            {
                logger.LogDebug("Database for tenant {TenantCode} is not provisioned yet. Skipping outbox dispatch.", tenant.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LogisticsFiscalDispatcher failed for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
        var integrationClient = scope.ServiceProvider.GetRequiredService<ITenantIntegrationClient>();

        if (!await TenantServiceReadiness.IsReadyAsync(db.Database.GetDbConnection(), cancellationToken))
            return;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "logistics_outbox"
                WHERE "EventType" IN ({0}, {1})
                  AND "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """,
                IntegrationConstants.LogisticsEvents.FiscalEmissionRequested,
                IntegrationConstants.LogisticsEvents.MdfeEmissionRequested)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        // Resolve config + adapter once per tenant batch
        using var config = await integrationClient.GetConfigAsync(tenant.Code, "fiscal", cancellationToken);
        if (config is null)
        {
            logger.LogDebug("No fiscal integration configured for tenant {TenantCode}. Skipping batch.", tenant.Code);
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        var emitter = BuildEmitter(config.Credentials);
        var adapter = FiscalAdapterBuilder.Build(config, httpClientFactory);

        // Build FocusNFe callback URL: {base}/api/logistics/webhooks/focusnfe/{tenantCode}?secret={secret}
        // Credential key "webhook_secret" must be set for webhook validation to work.
        var webhookBaseUrl = configuration["Logistics:WebhookBaseUrl"];
        config.Credentials.TryGetString("webhook_secret", out var webhookSecret);
        var focusNfeCallbackUrl = !string.IsNullOrEmpty(webhookBaseUrl) && !string.IsNullOrEmpty(webhookSecret)
            ? $"{webhookBaseUrl.TrimEnd('/')}/api/logistics/webhooks/focusnfe/{tenant.Code}?secret={Uri.EscapeDataString(webhookSecret)}"
            : null;

        foreach (var message in pending)
        {
            try
            {
                if (message.EventType == IntegrationConstants.LogisticsEvents.MdfeEmissionRequested)
                    await ProcessMdfeMessageAsync(db, adapter, emitter, message, tenant.Code, cancellationToken);
                else
                    await ProcessMessageAsync(db, adapter, emitter, focusNfeCallbackUrl, message, tenant.Code, cancellationToken);
                message.MarkDispatched();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Fiscal emission failed for message {MessageId} (tenant {TenantCode}).",
                    message.Id, tenant.Code);
                message.MarkTransientFailure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        LogisticsDbContext db,
        IFiscalIssuerAdapter adapter,
        NfeParty emitter,
        string? focusNfeCallbackUrl,
        LogisticsOutboxMessage message,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.Payload);
        var dispatchId = doc.RootElement.GetProperty("dispatchId").GetGuid();
        var shipmentOrderId = doc.RootElement.GetProperty("shipmentOrderId").GetGuid();

        var dispatch = await db.Dispatches.FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
        if (dispatch is null)
        {
            logger.LogWarning("Fiscal emission: dispatch {DispatchId} not found.", dispatchId);
            return;
        }

        if (!string.IsNullOrEmpty(dispatch.FiscalExternalId))
        {
            logger.LogInformation(
                "Dispatch {DispatchId} already has FiscalExternalId={ExternalId}. Skipping.",
                dispatchId, dispatch.FiscalExternalId);
            return;
        }

        var order = await db.ShipmentOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == shipmentOrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Fiscal emission: shipment order {OrderId} not found.", shipmentOrderId);
            return;
        }

        if (string.IsNullOrWhiteSpace(order.RecipientCnpj))
        {
            throw new InvalidOperationException($"Recipient CNPJ is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        if (string.IsNullOrWhiteSpace(order.RecipientState))
        {
            throw new InvalidOperationException($"Recipient State is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        if (string.IsNullOrWhiteSpace(order.RecipientCity) && string.IsNullOrWhiteSpace(order.DeliveryCity))
        {
            throw new InvalidOperationException($"Recipient City is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        if (string.IsNullOrWhiteSpace(order.RecipientStreet))
        {
            throw new InvalidOperationException($"Recipient Street address is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        if (string.IsNullOrWhiteSpace(order.RecipientDistrict))
        {
            throw new InvalidOperationException($"Recipient District is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        if (string.IsNullOrWhiteSpace(order.RecipientZipCode))
        {
            throw new InvalidOperationException($"Recipient ZIP Code is required in shipment order {order.OrderNumber} for NF-e emission.");
        }

        var recipient = new NfeParty(
            CnpjOrCpf: order.RecipientCnpj!,
            Name: order.RecipientName ?? "Destinatário",
            Email: order.RecipientEmail ?? string.Empty,
            Address: new NfeAddress(
                Street: order.RecipientStreet!,
                Number: order.RecipientNumber ?? "S/N",
                Complement: null,
                District: order.RecipientDistrict!,
                City: order.RecipientCity ?? order.DeliveryCity ?? string.Empty,
                State: order.RecipientState!,
                ZipCode: order.RecipientZipCode!),
            IeStateRegistration: order.RecipientIe);

        // Load fiscal profile to determine state transitions and default taxes
        var fiscalProfile = await db.FiscalProfiles.FirstOrDefaultAsync(cancellationToken);
        var ufOrigem = fiscalProfile?.UfOrigem ?? emitter.Address.State;
        var ufDestino = order.RecipientState ?? ufOrigem;
        var isInterestadual = !string.Equals(ufOrigem, ufDestino, StringComparison.OrdinalIgnoreCase);

        var defaultCfop = isInterestadual
            ? (fiscalProfile?.CfopPadraoInterestadual ?? "6102")
            : (fiscalProfile?.CfopPadraoIntraestadual ?? "5102");

        var items = order.Items.Select(i =>
        {
            if (string.IsNullOrWhiteSpace(i.NcmCode))
            {
                throw new InvalidOperationException($"NCM is required for item {i.MaterialCode} in shipment order {order.OrderNumber}.");
            }

            var icmsCst = string.IsNullOrEmpty(i.IcmsCst) ? (fiscalProfile?.IcmsCst ?? "40") : i.IcmsCst;
            // CST 40/41/50/60 = isento/não tributado/suspenso/ST retido → base e alíquota devem ser zero
            var isIcmsNaoTributado = icmsCst is "40" or "41" or "50" or "60";

            return new NfeItem(
                Code: i.MaterialCode,
                Description: i.MaterialCode,
                NcmCode: i.NcmCode,
                CfopCode: string.IsNullOrEmpty(i.CfopCode) ? defaultCfop : i.CfopCode,
                UnitOfMeasure: i.UnitOfMeasure,
                Quantity: i.Quantity,
                UnitValue: i.UnitValue,
                TaxBaseIcms: isIcmsNaoTributado ? 0m : i.TaxBaseIcms,
                IcmsRate: isIcmsNaoTributado ? 0m : i.IcmsRate,
                IpiRate: i.IpiRate,
                IcmsOrigin: i.IcmsOrigin,
                IcmsCst: icmsCst,
                PisCst: string.IsNullOrEmpty(i.PisCst) ? (fiscalProfile?.PisCst ?? "07") : i.PisCst,
                CofinsCst: string.IsNullOrEmpty(i.CofinsCst) ? (fiscalProfile?.CofinsCst ?? "07") : i.CofinsCst,
                IpiCst: string.IsNullOrEmpty(i.IpiCst) ? (i.IpiRate > 0 ? "50" : "99") : i.IpiCst);
        }).ToList();

        var nfeRequest = new NfeRequest(
            TenantId: tenantCode,
            RefCode: $"NF-{dispatch.TrackingCode}",
            NatureOfOperation: order.NatureOfOperation,
            Emitter: emitter,
            Recipient: recipient,
            Items: items,
            WebhookCallbackUrl: focusNfeCallbackUrl,
            ModalidadeFrete: order.ModalidadeFrete);

        var result = await adapter.EmitirNfeAsync(nfeRequest, cancellationToken);
        dispatch.UpdateFiscalStatus(result.ExternalId, result.Status, result.AccessKey);

        // If NF-e is authorized synchronously (mock) and vehicle data is present, queue MDF-e
        if (IsNfeAuthorized(result.Status) && !string.IsNullOrEmpty(dispatch.VehiclePlate))
        {
            var mdfePayload = JsonSerializer.Serialize(new
            {
                dispatchId = dispatch.Id,
                shipmentOrderId = dispatch.ShipmentOrderId,
                nfeAccessKey = result.AccessKey,
            });
            db.OutboxMessages.Add(LogisticsOutboxMessage.Create(
                IntegrationConstants.LogisticsEvents.MdfeEmissionRequested, mdfePayload));
        }

        logger.LogInformation(
            "NF-e emitted for dispatch {DispatchId}: externalId={ExternalId} status={Status}",
            dispatchId, result.ExternalId, result.Status);
    }

    private static bool IsNfeAuthorized(string status) =>
        status is "autorizado" or "CONCLUIDO" or "authorized";

    private async Task ProcessMdfeMessageAsync(
        LogisticsDbContext db,
        IFiscalIssuerAdapter adapter,
        NfeParty emitter,
        LogisticsOutboxMessage message,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.Payload);
        var dispatchId = doc.RootElement.GetProperty("dispatchId").GetGuid();
        var nfeAccessKey = doc.RootElement.TryGetProperty("nfeAccessKey", out var ak) ? ak.GetString() : null;

        var dispatch = await db.Dispatches.FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
        if (dispatch is null)
        {
            logger.LogWarning("MDF-e emission: dispatch {DispatchId} not found.", dispatchId);
            return;
        }

        if (!string.IsNullOrEmpty(dispatch.MdfeExternalId))
        {
            logger.LogInformation("Dispatch {DispatchId} already has MdfeExternalId. Skipping.", dispatchId);
            return;
        }

        if (string.IsNullOrEmpty(dispatch.VehiclePlate) || string.IsNullOrEmpty(dispatch.DriverCpf))
        {
            throw new InvalidOperationException($"Vehicle plate and driver CPF are required for dispatch {dispatchId} to emit MDF-e.");
        }

        var order = await db.ShipmentOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dispatch.ShipmentOrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("MDF-e emission: shipment order {OrderId} not found.", dispatch.ShipmentOrderId);
            return;
        }

        // Load fiscal profile for UF origin and tenant CNPJ
        var fiscalProfile = await db.FiscalProfiles.FirstOrDefaultAsync(cancellationToken);
        var ufOrigem = fiscalProfile?.UfOrigem ?? emitter.Address.State;
        var ufDestino = order.RecipientState ?? ufOrigem;

        // percUF deve conter apenas UFs intermediárias — nunca a UF de carregamento nem descarregamento
        var ufsPercorridas = new List<string>();

        var totalKg = order.Items.Sum(i => i.WeightKg * i.Quantity);
        var totalValue = order.Items.Sum(i => i.UnitValue * i.Quantity);

        var resolvedNfeAccessKey = string.IsNullOrEmpty(nfeAccessKey) ? dispatch.FiscalAccessKey : nfeAccessKey;
        if (string.IsNullOrEmpty(resolvedNfeAccessKey))
        {
            throw new InvalidOperationException($"MDF-e emission: NF-e access key is missing for dispatch {dispatchId}.");
        }
        var nfeLinks = new[] { new MdfeNfeLink(resolvedNfeAccessKey) };

        var mdfeRequest = new MdfeRequest(
            TenantId: emitter.CnpjOrCpf,
            RefCode: $"MDFE-{dispatch.TrackingCode}",
            Vehicle: new MdfeVehicle(dispatch.VehiclePlate, dispatch.VehicleRntrc),
            Driver: new MdfeDriver(dispatch.DriverCpf, dispatch.DriverName ?? "Motorista"),
            UfInicio: ufOrigem,
            UfFim: ufDestino,
            UfsPercorridas: ufsPercorridas,
            TotalWeightKg: totalKg,
            TotalValueBrl: totalValue,
            NfeLinks: nfeLinks);

        var result = await adapter.EmitirMdfeAsync(mdfeRequest, cancellationToken);
        dispatch.UpdateMdfeStatus(result.ExternalId, result.Status, result.AccessKey, result.ErrorMessage, resolvedNfeAccessKey, ufOrigem, ufDestino);

        logger.LogInformation(
            "MDF-e emitted for dispatch {DispatchId}: externalId={ExternalId} status={Status}",
            dispatchId, result.ExternalId, result.Status);
    }

    private static NfeParty BuildEmitter(SecureCredentials credentials)
    {
        credentials.TryGetString("emitter_cnpj", out var cnpj);
        credentials.TryGetString("emitter_name", out var name);
        credentials.TryGetString("emitter_email", out var email);
        credentials.TryGetString("emitter_ie", out var ie);
        credentials.TryGetString("emitter_street", out var street);
        credentials.TryGetString("emitter_number", out var number);
        credentials.TryGetString("emitter_complement", out var complement);
        credentials.TryGetString("emitter_district", out var district);
        credentials.TryGetString("emitter_city", out var city);
        credentials.TryGetString("emitter_city_ibge", out var cityIbge);
        credentials.TryGetString("emitter_state", out var state);
        credentials.TryGetString("emitter_zip", out var zip);

        return new NfeParty(
            CnpjOrCpf: cnpj,
            Name: string.IsNullOrEmpty(name) ? "Emitente" : name,
            Email: email,
            Address: new NfeAddress(
                Street: street,
                Number: string.IsNullOrEmpty(number) ? "S/N" : number,
                Complement: string.IsNullOrEmpty(complement) ? null : complement,
                District: district,
                City: city,
                State: state,
                ZipCode: zip,
                CityIbgeCode: string.IsNullOrEmpty(cityIbge) ? null : cityIbge),
            IeStateRegistration: string.IsNullOrEmpty(ie) ? null : ie);
    }
}
