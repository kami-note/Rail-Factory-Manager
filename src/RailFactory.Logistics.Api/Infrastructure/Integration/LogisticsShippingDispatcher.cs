using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Integrations;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

public sealed class LogisticsShippingDispatcher(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<LogisticsShippingDispatcher> logger) : BackgroundService
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
                logger.LogError(ex, "LogisticsShippingDispatcher encountered an unexpected error.");
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
                logger.LogDebug("Database for tenant {TenantCode} is not provisioned yet. Skipping shipping dispatch.", tenant.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "LogisticsShippingDispatcher failed for tenant {TenantCode}.", tenant.Code);
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
                WHERE "EventType" = {0}
                  AND "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 20
                FOR UPDATE SKIP LOCKED
                """, IntegrationConstants.LogisticsEvents.ShippingLabelRequested)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        using var config = await integrationClient.GetConfigAsync(tenant.Code, "shipping", cancellationToken);

        if (config is null)
        {
            logger.LogDebug("No shipping integration configured for tenant {TenantCode}. Skipping batch.", tenant.Code);
            await tx.RollbackAsync(cancellationToken);
            return;
        }

        var adapter = ShippingAdapterBuilder.Build(config, httpClientFactory);
        var sender = BuildSender(config.Credentials);

        foreach (var message in pending)
        {
            try
            {
                await ProcessMessageAsync(db, adapter, sender, message, tenant.Code, cancellationToken);
                message.MarkDispatched();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Shipping label request failed for message {MessageId} (tenant {TenantCode}).",
                    message.Id, tenant.Code);
                if (message.AttemptCount >= 4)
                    message.MarkDeadLetter(ex.Message);
                else
                    message.MarkTransientFailure(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        LogisticsDbContext db,
        IShippingAdapter adapter,
        ShippingAddress? sender,
        Domain.LogisticsOutboxMessage message,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(message.Payload);
        var dispatchId = doc.RootElement.GetProperty("dispatchId").GetGuid();
        var shipmentOrderId = doc.RootElement.GetProperty("shipmentOrderId").GetGuid();

        var dispatch = await db.Dispatches.FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken);
        if (dispatch is null)
        {
            logger.LogWarning("Shipping: dispatch {DispatchId} not found.", dispatchId);
            return;
        }

        if (!string.IsNullOrEmpty(dispatch.ShippingExternalId))
        {
            logger.LogInformation(
                "Dispatch {DispatchId} already has ShippingExternalId={Id}. Skipping.",
                dispatchId, dispatch.ShippingExternalId);
            return;
        }

        var order = await db.ShipmentOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == shipmentOrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Shipping: shipment order {OrderId} not found.", shipmentOrderId);
            return;
        }

        var recipient = new ShippingAddress(
            Name: order.RecipientName ?? "Destinatário",
            Phone: "00000000000",
            Document: order.RecipientCnpj ?? string.Empty,
            ZipCode: order.RecipientZipCode ?? string.Empty,
            Street: order.RecipientStreet ?? string.Empty,
            Number: order.RecipientNumber ?? "S/N",
            Complement: null,
            District: order.RecipientDistrict ?? string.Empty,
            City: order.RecipientCity ?? order.DeliveryCity ?? string.Empty,
            StateAbbr: order.RecipientState ?? string.Empty);

        var from = sender ?? recipient; // fallback when no shipping integration configured

        var packages = order.Items.Any()
            ? order.Items.Select(i => new ShippingPackage(
                WeightKg: i.WeightKg > 0 ? i.WeightKg * i.Quantity : 0.3m * i.Quantity,
                HeightCm: 10m, WidthCm: 15m, LengthCm: 20m)).ToList()
            : new List<ShippingPackage> { new(0.3m, 10m, 15m, 20m) };

        var insuredValue = order.Items.Sum(i => i.UnitValue * i.Quantity);

        var labelRequest = new ShippingLabelRequest(
            TenantId: tenantCode,
            ReferenceCode: dispatch.TrackingCode,
            From: from,
            To: recipient,
            Packages: packages,
            InsuredValueBrl: insuredValue > 0 ? insuredValue : 1m);

        var result = await adapter.RequestLabelAsync(labelRequest, cancellationToken);

        if (result.Status == "error")
            throw new InvalidOperationException(result.ErrorMessage ?? "Shipping provider returned an error.");

        dispatch.UpdateShippingStatus(result.ExternalId, result.Status, result.LabelUrl, result.TrackingCode, result.ErrorMessage);

        logger.LogInformation(
            "Shipping label requested for dispatch {DispatchId}: externalId={Id} status={Status}",
            dispatchId, result.ExternalId, result.Status);
    }

    private static ShippingAddress BuildSender(RailFactory.BuildingBlocks.Integrations.SecureCredentials credentials)
    {
        credentials.TryGetString("sender_name", out var name);
        credentials.TryGetString("sender_document", out var document);
        credentials.TryGetString("sender_phone", out var phone);
        credentials.TryGetString("sender_zip_code", out var zip);
        credentials.TryGetString("sender_street", out var street);
        credentials.TryGetString("sender_number", out var number);
        credentials.TryGetString("sender_complement", out var complement);
        credentials.TryGetString("sender_district", out var district);
        credentials.TryGetString("sender_city", out var city);
        credentials.TryGetString("sender_state", out var state);

        return new ShippingAddress(
            Name: string.IsNullOrEmpty(name) ? "Remetente" : name,
            Phone: string.IsNullOrEmpty(phone) ? "00000000000" : phone,
            Document: document ?? string.Empty,
            ZipCode: zip ?? string.Empty,
            Street: street ?? string.Empty,
            Number: string.IsNullOrEmpty(number) ? "S/N" : number,
            Complement: string.IsNullOrEmpty(complement) ? null : complement,
            District: district ?? string.Empty,
            City: city ?? string.Empty,
            StateAbbr: state ?? string.Empty);
    }
}
