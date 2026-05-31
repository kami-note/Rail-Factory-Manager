using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Domain;
using RailFactory.Production.Api.Infrastructure.Persistence;

namespace RailFactory.Production.Api.Infrastructure.Integration;

/// <summary>
/// Background service that reads pending Production outbox messages and publishes per-item
/// <c>production.stock_reservation_requested</c> events to the <c>railfactory.production</c>
/// exchange. Each BOM item becomes an independent message so the Inventory consumer can
/// reserve stock atomically per material.
/// </summary>
public sealed class ProductionInventoryDispatcher(
    IServiceProvider serviceProvider,
    RabbitMqPublisher publisher,
    ILogger<ProductionInventoryDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

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
                logger.LogError(ex, "Failed to dispatch production outbox messages.");
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
                logger.LogError(ex, "Failed to dispatch production outbox messages for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<ProductionDbContext>();

        // SKIP LOCKED ensures multiple dispatcher instances never process the same row.
        // The transaction is held until SaveChanges commits, then the row locks are released.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pendingMessages = await dbContext.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "production_outbox"
                WHERE "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            if (message.EventType == IntegrationConstants.ProductionEvents.ProductionOrderReleased)
                await HandleOrderReleasedAsync(message, tenant, dbContext, cancellationToken);
            else if (message.EventType == IntegrationConstants.ProductionEvents.ProductionOrderCompleted)
                await HandleOrderStatusChangedAsync(message, tenant, IntegrationConstants.ProductionEvents.OrderCompleted, cancellationToken);
            else if (message.EventType == IntegrationConstants.ProductionEvents.ProductionOrderCancelled)
                await HandleOrderStatusChangedAsync(message, tenant, IntegrationConstants.ProductionEvents.OrderCancelled, cancellationToken);
            else
                logger.LogWarning("Unknown production outbox event type {EventType}.", message.EventType);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleOrderReleasedAsync(
        ProductionOutboxMessage message,
        TenantResolutionResult tenant,
        ProductionDbContext dbContext,
        CancellationToken cancellationToken)
    {
        OrderReleasedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OrderReleasedPayload>(message.Payload, CaseInsensitiveOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize production outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON: {ex.Message}");
            return;
        }

        if (payload is null)
        {
            message.MarkDeadLetter("Payload deserialized to null.");
            return;
        }

        var bom = await dbContext.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == payload.BomId, cancellationToken);

        if (bom is null)
        {
            message.MarkDeadLetter($"BOM '{payload.BomId}' not found for order '{payload.OrderId}'.");
            return;
        }

        var allSucceeded = true;

        foreach (var item in bom.Items)
        {
            var requiredQuantity = item.Quantity * payload.PlannedQuantity;

            // Deterministic EventId: derived from outbox message ID + BOM item ID.
            // Guarantees idempotency even if the dispatcher crashes mid-batch and retries.
            var eventId = CreateDeterministicId(message.Id, item.Id);

            var itemPayload = JsonSerializer.SerializeToElement(new
            {
                productionOrderId = payload.OrderId,
                orderNumber = payload.OrderNumber,
                materialCode = item.MaterialCode.Value,
                requiredQuantity,
                unitOfMeasure = item.UnitOfMeasure
            });

            var envelope = new RabbitMqEnvelope(
                EventId: eventId,
                EventType: IntegrationConstants.ProductionEvents.StockReservationRequested,
                CorrelationId: message.Id.ToString(),
                TenantCode: tenant.Code,
                Payload: itemPayload,
                OccurredAt: message.OccurredAt);

            try
            {
                await publisher.PublishAsync(
                    IntegrationConstants.ProductionEvents.StockReservationRequested,
                    envelope,
                    cancellationToken);

                logger.LogDebug(
                    "Published stock reservation for material {MaterialCode} in order {OrderNumber} (EventId: {EventId}).",
                    item.MaterialCode.Value, payload.OrderNumber, eventId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to publish stock reservation for outbox message {MessageId}.", message.Id);
                allSucceeded = false;
                break;
            }
        }

        if (allSucceeded)
        {
            message.MarkDispatched();
            logger.LogInformation(
                "Dispatched production_order_released for order {OrderNumber} ({ItemCount} items).",
                payload.OrderNumber, bom.Items.Count);
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
    /// Publishes a single-message event for order completion or cancellation.
    /// The Inventory consumer routes these to ConsumeReservedStock / ReleaseOrderReservation.
    /// </summary>
    private async Task HandleOrderStatusChangedAsync(
        ProductionOutboxMessage message,
        TenantResolutionResult tenant,
        string integrationEventType,
        CancellationToken cancellationToken)
    {
        OrderStatusChangedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OrderStatusChangedPayload>(message.Payload, CaseInsensitiveOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize production outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON: {ex.Message}");
            return;
        }

        if (payload is null)
        {
            message.MarkDeadLetter("Payload deserialized to null.");
            return;
        }

        var itemPayload = JsonSerializer.SerializeToElement(new
        {
            productionOrderId = payload.OrderId,
            orderNumber = payload.OrderNumber
        });

        var envelope = new RabbitMqEnvelope(
            EventId: message.Id,
            EventType: integrationEventType,
            CorrelationId: message.Id.ToString(),
            TenantCode: tenant.Code,
            Payload: itemPayload,
            OccurredAt: message.OccurredAt);

        try
        {
            await publisher.PublishAsync(integrationEventType, envelope, cancellationToken);
            message.MarkDispatched();
            logger.LogInformation(
                "Dispatched {EventType} for order {OrderNumber}.",
                integrationEventType, payload.OrderNumber);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to publish {EventType} for outbox message {MessageId}.", integrationEventType, message.Id);
            if (message.AttemptCount + 1 >= MaxTransientAttempts)
                message.MarkDeadLetter("Max transient retry attempts exceeded.");
            else
                message.MarkTransientFailure("RabbitMQ publish failed. Will retry.");
        }
    }

    /// <summary>
    /// Creates a deterministic <see cref="Guid"/> from two source GUIDs using MD5.
    /// Guarantees the same EventId is produced on retry — required for Inventory idempotency.
    /// </summary>
    private static Guid CreateDeterministicId(Guid outboxId, Guid itemId)
    {
        Span<byte> data = stackalloc byte[32];
        outboxId.TryWriteBytes(data[..16]);
        itemId.TryWriteBytes(data[16..]);
        var hash = MD5.HashData(data);
        return new Guid(hash);
    }

    private sealed record OrderReleasedPayload(
        Guid OrderId,
        string OrderNumber,
        string ProductCode,
        Guid BomId,
        Guid WorkCenterId,
        decimal PlannedQuantity,
        DateTimeOffset OccurredAt);

    private sealed record OrderStatusChangedPayload(
        Guid OrderId,
        string OrderNumber);
}
