using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

/// <summary>
/// Background service that reads pending Supply Chain outbox messages and publishes them
/// as <see cref="RabbitMqEnvelope"/> messages to the <c>railfactory.supply-chain</c> exchange.
/// The Inventory integration consumer receives these messages and processes them asynchronously.
/// </summary>
public sealed class InventoryPendingBalanceDispatcher(
    IServiceProvider serviceProvider,
    RabbitMqPublisher publisher,
    ILogger<InventoryPendingBalanceDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;

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
                logger.LogError(ex, "Failed to dispatch supply outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
                logger.LogError(ex, "Failed to dispatch supply outbox messages for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();

        // SKIP LOCKED ensures multiple dispatcher instances never process the same row.
        // The transaction is held until SaveChanges commits, then the row locks are released.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pendingMessages = await dbContext.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "supply_outbox_messages"
                WHERE "Status" = 'Pending'
                ORDER BY "CreatedAt" ASC
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            var routingKey = message.EventType;

            if (routingKey is not (
                IntegrationConstants.Events.ReceiptItemRegistered or
                IntegrationConstants.Events.ReceiptItemConferred or
                IntegrationConstants.Events.SupplierMaterialMappingCreated))
            {
                logger.LogWarning("Unknown event type {EventType} in supply outbox. Sending to dead-letter.", message.EventType);
                message.MarkDeadLetter($"Unknown event type: {message.EventType}");
                continue;
            }

            var payload = DeserializePayload(message);
            if (payload is null) continue;

            var envelope = new RabbitMqEnvelope(
                EventId: message.Id,
                EventType: message.EventType,
                CorrelationId: message.CorrelationId,
                TenantCode: tenant.Code,
                Payload: payload.Value,
                OccurredAt: message.CreatedAt);

            try
            {
                await publisher.PublishAsync(routingKey, envelope, cancellationToken);
                message.MarkDispatched();
                logger.LogDebug("Published supply event {EventType} for tenant {TenantCode} (OutboxId: {OutboxId}).",
                    message.EventType, tenant.Code, message.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to publish outbox message {MessageId} to RabbitMQ.", message.Id);
                MarkTransientFailure(message, ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private JsonElement? DeserializePayload(SupplyOutboxMessage message)
    {
        try
        {
            using var document = JsonDocument.Parse(message.PayloadJson);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON payload for outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON payload: {ex.Message}");
            return null;
        }
    }

    private static void MarkTransientFailure(SupplyOutboxMessage message, string error)
    {
        if (message.AttemptCount + 1 >= MaxTransientAttempts)
        {
            message.MarkDeadLetter($"Transient retry limit exceeded. Last error: {error}");
            return;
        }

        message.MarkTransientFailure(error);
    }
}
