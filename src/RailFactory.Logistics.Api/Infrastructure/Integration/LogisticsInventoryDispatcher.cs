using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

/// <summary>
/// Background service that reads pending Logistics outbox messages and publishes per-item
/// <c>logistics.shipment_dispatched</c> events to the <c>railfactory.logistics</c> exchange.
/// </summary>
public sealed class LogisticsInventoryDispatcher(
    IServiceProvider serviceProvider,
    RabbitMqPublisher publisher,
    ILogger<LogisticsInventoryDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
                logger.LogError(ex, "Failed to dispatch logistics outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task DispatchAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var activeTenants = await catalogClient.ListAllAsync(cancellationToken);

        foreach (var tenant in activeTenants.Where(x => x.IsActive))
        {
            try
            {
                await DispatchTenantBatchAsync(tenant, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch logistics outbox for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pendingMessages = await dbContext.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "logistics_outbox"
                WHERE "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            if (message.EventType == IntegrationConstants.LogisticsEvents.ShipmentDispatched)
                await HandleShipmentDispatchedAsync(message, tenant, cancellationToken);
            else
                logger.LogWarning("Unknown logistics outbox event type {EventType}.", message.EventType);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleShipmentDispatchedAsync(
        Domain.LogisticsOutboxMessage message,
        TenantResolutionResult tenant,
        CancellationToken cancellationToken)
    {
        ShipmentDispatchedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ShipmentDispatchedPayload>(message.Payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize logistics outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON: {ex.Message}");
            return;
        }

        if (payload is null || payload.Items is null || payload.Items.Count == 0)
        {
            message.MarkDeadLetter("Payload is null or contains no items.");
            return;
        }

        var allSucceeded = true;

        foreach (var item in payload.Items)
        {
            var eventId = CreateDeterministicId(message.Id, item.ItemId);

            var itemPayload = JsonSerializer.SerializeToElement(new
            {
                dispatchId = payload.DispatchId,
                trackingCode = payload.TrackingCode,
                shipmentOrderId = payload.ShipmentOrderId,
                orderNumber = payload.OrderNumber,
                materialCode = item.MaterialCode,
                quantity = item.Quantity,
                unitOfMeasure = item.UnitOfMeasure
            });

            var envelope = new RabbitMqEnvelope(
                EventId: eventId,
                EventType: IntegrationConstants.LogisticsEvents.ShipmentDispatched,
                CorrelationId: message.Id.ToString(),
                TenantCode: tenant.Code,
                Payload: itemPayload,
                OccurredAt: message.OccurredAt);

            try
            {
                await publisher.PublishAsync(
                    IntegrationConstants.LogisticsEvents.ShipmentDispatched,
                    envelope,
                    cancellationToken);

                logger.LogDebug(
                    "Published shipment_dispatched for material {MaterialCode} / tracking {TrackingCode} (EventId: {EventId}).",
                    item.MaterialCode, payload.TrackingCode, eventId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to publish shipment_dispatched for outbox message {MessageId}.", message.Id);
                allSucceeded = false;
                break;
            }
        }

        if (allSucceeded)
        {
            message.MarkDispatched();
            logger.LogInformation(
                "Dispatched shipment_dispatched for order {OrderNumber} ({ItemCount} items).",
                payload.OrderNumber, payload.Items.Count);
        }
        else if (message.AttemptCount + 1 >= MaxTransientAttempts)
        {
            message.MarkDeadLetter("Max transient retry attempts exceeded.");
        }
        else
        {
            message.MarkTransientFailure("RabbitMQ publish failed. Will retry.");
        }
    }

    /// <summary>
    /// Creates a deterministic Guid from two source GUIDs using MD5.
    /// Guarantees the same EventId on retry — required for Inventory idempotency.
    /// </summary>
    private static Guid CreateDeterministicId(Guid outboxId, Guid itemId)
    {
        Span<byte> data = stackalloc byte[32];
        outboxId.TryWriteBytes(data[..16]);
        itemId.TryWriteBytes(data[16..]);
        var hash = MD5.HashData(data);
        return new Guid(hash);
    }

    private sealed record ShipmentDispatchedPayload(
        Guid DispatchId,
        string TrackingCode,
        Guid ShipmentOrderId,
        string OrderNumber,
        List<ShipmentItemInfo> Items);

    private sealed record ShipmentItemInfo(
        Guid ItemId,
        string MaterialCode,
        decimal Quantity,
        string UnitOfMeasure);
}
