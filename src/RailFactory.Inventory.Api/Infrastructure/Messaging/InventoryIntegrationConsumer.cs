using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Materials;

namespace RailFactory.Inventory.Api.Infrastructure.Messaging;

/// <summary>
/// BackgroundService that consumes integration events published by SupplyChain and Production
/// to the Inventory RabbitMQ queues and routes them to the appropriate use-cases.
/// </summary>
/// <remarks>
/// Ack/Nack policy:
/// - Success (including idempotent skip): BasicAck.
/// - Known business error (domain invariant violation): BasicNack, requeue=false → DLX.
/// - Transient error (DB unavailable, etc.): BasicNack, requeue=true.
/// </remarks>
public sealed class InventoryIntegrationConsumer(
    IConnection connection,
    IServiceProvider serviceProvider,
    ILogger<InventoryIntegrationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inventory integration consumer starting.");

        // Two channels — one per queue — so supply and production messages are processed independently.
        var supplyChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var productionChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        try
        {
            await supplyChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);
            await productionChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

            var supplyConsumer = new AsyncEventingBasicConsumer(supplyChannel);
            supplyConsumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, supplyChannel, stoppingToken);
            await supplyChannel.BasicConsumeAsync(
                IntegrationConstants.Queues.InventorySupplyIntegration,
                autoAck: false,
                consumer: supplyConsumer,
                cancellationToken: stoppingToken);

            var productionConsumer = new AsyncEventingBasicConsumer(productionChannel);
            productionConsumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, productionChannel, stoppingToken);
            await productionChannel.BasicConsumeAsync(
                IntegrationConstants.Queues.InventoryProductionIntegration,
                autoAck: false,
                consumer: productionConsumer,
                cancellationToken: stoppingToken);

            logger.LogInformation("Inventory integration consumer listening on supply and production queues.");

            // Block until shutdown is requested.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        finally
        {
            await supplyChannel.DisposeAsync();
            await productionChannel.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, IChannel channel, CancellationToken cancellationToken)
    {
        RabbitMqEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<RabbitMqEnvelope>(ea.Body.Span, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize RabbitMQ message (tag {DeliveryTag}). Sending to dead-letter.", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            return;
        }

        if (envelope is null)
        {
            logger.LogWarning("Received null envelope (tag {DeliveryTag}). Sending to dead-letter.", ea.DeliveryTag);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            return;
        }

        try
        {
            await RouteMessageAsync(envelope, cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (IsBusinessException(ex))
        {
            logger.LogError(ex,
                "Business error processing event {EventType} (EventId: {EventId}, Tenant: {Tenant}). Sending to dead-letter.",
                envelope.EventType, envelope.EventId, envelope.TenantCode);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex,
                "Transient error processing event {EventType} (EventId: {EventId}, Tenant: {Tenant}). Requeuing.",
                envelope.EventType, envelope.EventId, envelope.TenantCode);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
        }
    }

    private async Task RouteMessageAsync(RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        // Resolve and bind tenant context so repository calls hit the correct DB.
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var tenant = await catalogClient.ResolveAsync(envelope.TenantCode, cancellationToken);

        if (!tenant.IsActive)
            throw new InvalidOperationException($"Tenant '{envelope.TenantCode}' is not active or does not exist.");

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        _ = envelope.EventType switch
        {
            IntegrationConstants.Events.ReceiptItemRegistered
                => await HandlePendingBalanceAsync(scope, envelope, cancellationToken),
            IntegrationConstants.Events.ReceiptItemConferred
                => await HandleConfirmedBalanceAsync(scope, envelope, cancellationToken),
            IntegrationConstants.Events.SupplierMaterialMappingCreated
                => await HandleSupplierMappingAsync(scope, envelope, cancellationToken),
            IntegrationConstants.ProductionEvents.StockReservationRequested
                => await HandleStockReservationAsync(scope, envelope, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown event type: {envelope.EventType}")
        };
    }

    // ─── Handlers ────────────────────────────────────────────────────────────

    private async Task<bool> HandlePendingBalanceAsync(IServiceScope scope, RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = envelope.Payload.Deserialize<PendingBalancePayload>(JsonOptions)
            ?? throw new ArgumentException("PendingBalance payload is null.");

        var useCase = scope.ServiceProvider.GetRequiredService<CreatePendingBalance>();
        return await useCase.ExecuteAsync(new CreatePendingBalanceInput(
            EventId: envelope.EventId,
            EventType: envelope.EventType,
            CorrelationId: envelope.CorrelationId.ToString(),
            ReceiptId: payload.ReceiptId,
            ReceiptItemId: payload.ReceiptItemId,
            ReceiptNumber: payload.ReceiptNumber,
            MaterialCode: payload.MaterialCode,
            Quantity: payload.Quantity,
            UnitOfMeasure: payload.UnitOfMeasure,
            UnitPrice: payload.UnitPrice,
            OriginalDescription: payload.OriginalDescription,
            AccessKey: payload.AccessKey,
            SupplierName: payload.SupplierName,
            Source: string.IsNullOrWhiteSpace(payload.Source) ? "supply-chain" : payload.Source,
            Ncm: payload.Ncm,
            Gtin: payload.Gtin),
            cancellationToken);
    }

    private async Task<bool> HandleConfirmedBalanceAsync(IServiceScope scope, RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = envelope.Payload.Deserialize<BalanceConfirmationPayload>(JsonOptions)
            ?? throw new ArgumentException("BalanceConfirmation payload is null.");

        var useCase = scope.ServiceProvider.GetRequiredService<ConfirmInventoryBalance>();
        return await useCase.ExecuteAsync(new ConfirmInventoryBalanceInput(
            EventId: envelope.EventId,
            EventType: envelope.EventType,
            CorrelationId: envelope.CorrelationId.ToString(),
            ReceiptId: payload.ReceiptId,
            ReceiptItemId: payload.ReceiptItemId,
            ReceiptStatus: payload.ReceiptStatus,
            IsApproved: payload.IsItemApproved,
            CountedQuantity: payload.CountedQuantity,
            LotNumber: payload.LotNumber,
            ExpirationDate: payload.ExpirationDate),
            cancellationToken);
    }

    private async Task<bool> HandleSupplierMappingAsync(IServiceScope scope, RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = envelope.Payload.Deserialize<SupplierMaterialMappingPayload>(JsonOptions)
            ?? throw new ArgumentException("SupplierMaterialMapping payload is null.");

        var useCase = scope.ServiceProvider.GetRequiredService<RegisterSupplierMaterialMapping>();
        return await useCase.ExecuteAsync(new RegisterSupplierMaterialMappingInput(
            SupplierFiscalId: payload.SupplierFiscalId,
            SupplierProductCode: payload.SupplierProductCode,
            MaterialCode: payload.MaterialCode),
            cancellationToken);
    }

    private async Task<bool> HandleStockReservationAsync(IServiceScope scope, RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = envelope.Payload.Deserialize<StockReservationPayload>(JsonOptions)
            ?? throw new ArgumentException("StockReservation payload is null.");

        var useCase = scope.ServiceProvider.GetRequiredService<ReserveInventoryBalance>();
        return await useCase.ExecuteAsync(new ReserveInventoryBalanceInput(
            EventId: envelope.EventId,
            EventType: envelope.EventType,
            CorrelationId: envelope.CorrelationId.ToString(),
            ProductionOrderId: payload.ProductionOrderId,
            OrderNumber: payload.OrderNumber,
            MaterialCode: payload.MaterialCode,
            RequiredQuantity: payload.RequiredQuantity,
            UnitOfMeasure: payload.UnitOfMeasure),
            cancellationToken);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Domain/argument exceptions indicate bad data → dead-letter, do not requeue.</summary>
    private static bool IsBusinessException(Exception ex) =>
        ex is InvalidOperationException or ArgumentException;

    // ─── Payload DTOs ─────────────────────────────────────────────────────────

    private sealed record PendingBalancePayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptNumber,
        string MaterialCode,
        decimal Quantity,
        string UnitOfMeasure,
        decimal? UnitPrice,
        string? OriginalDescription,
        string? AccessKey,
        string? Source,
        string? SupplierName,
        string? Ncm,
        string? Gtin);

    private sealed record BalanceConfirmationPayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptStatus,
        bool IsItemApproved,
        decimal CountedQuantity,
        string? LotNumber,
        DateTimeOffset? ExpirationDate,
        string? Source);

    private sealed record SupplierMaterialMappingPayload(
        string SupplierFiscalId,
        string SupplierProductCode,
        string MaterialCode);

    private sealed record StockReservationPayload(
        Guid ProductionOrderId,
        string OrderNumber,
        string MaterialCode,
        decimal RequiredQuantity,
        string UnitOfMeasure);
}
