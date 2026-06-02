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

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var pending = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "logistics_outbox"
                WHERE "EventType" = {0}
                  AND "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """, IntegrationConstants.LogisticsEvents.FiscalEmissionRequested)
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
            logger.LogWarning(
                "Dispatch {DispatchId}: recipient CNPJ missing in shipment order. Skipping NF-e emission.",
                dispatchId);
            return;
        }

        var recipient = new NfeParty(
            CnpjOrCpf: order.RecipientCnpj!,
            Name: order.RecipientName ?? "Destinatário",
            Email: order.RecipientEmail ?? string.Empty,
            Address: new NfeAddress(
                Street: order.RecipientStreet ?? string.Empty,
                Number: order.RecipientNumber ?? "S/N",
                Complement: null,
                District: order.RecipientDistrict ?? string.Empty,
                City: order.RecipientCity ?? order.DeliveryCity ?? string.Empty,
                State: order.RecipientState ?? string.Empty,
                ZipCode: order.RecipientZipCode ?? string.Empty));

        var items = order.Items.Select(i => new NfeItem(
            Code: i.MaterialCode,
            Description: i.MaterialCode,
            NcmCode: string.IsNullOrEmpty(i.NcmCode) ? "00000000" : i.NcmCode,
            CfopCode: string.IsNullOrEmpty(i.CfopCode) ? "5102" : i.CfopCode,
            UnitOfMeasure: i.UnitOfMeasure,
            Quantity: i.Quantity,
            UnitValue: i.UnitValue,
            TaxBaseIcms: i.TaxBaseIcms,
            IcmsRate: i.IcmsRate)).ToList();

        var nfeRequest = new NfeRequest(
            TenantId: tenantCode,
            RefCode: $"NF-{dispatch.TrackingCode}",
            NatureOfOperation: order.NatureOfOperation,
            Emitter: emitter,
            Recipient: recipient,
            Items: items,
            WebhookCallbackUrl: focusNfeCallbackUrl);

        var result = await adapter.EmitirNfeAsync(nfeRequest, cancellationToken);
        dispatch.UpdateFiscalStatus(result.ExternalId, result.Status, result.AccessKey);

        logger.LogInformation(
            "NF-e emitted for dispatch {DispatchId}: externalId={ExternalId} status={Status}",
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
                ZipCode: zip),
            IeStateRegistration: string.IsNullOrEmpty(ie) ? null : ie);
    }
}
