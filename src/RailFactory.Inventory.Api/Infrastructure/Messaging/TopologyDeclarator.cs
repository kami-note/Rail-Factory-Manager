using RabbitMQ.Client;
using RailFactory.BuildingBlocks.Events;

namespace RailFactory.Inventory.Api.Infrastructure.Messaging;

/// <summary>
/// Hosted service that idempotently declares all RabbitMQ exchanges, queues and bindings
/// required by the Inventory integration consumer.
/// Runs once at application startup — all declarations are passive-safe (passive: false, durable: true).
/// </summary>
public sealed class TopologyDeclarator(
    IConnection connection,
    ILogger<TopologyDeclarator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Declaring RabbitMQ topology for Inventory integration.");

        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Dead-letter exchange and queue — catches nacked messages from both integration queues.
        await channel.ExchangeDeclareAsync(
            IntegrationConstants.Exchanges.DeadLetter,
            ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            IntegrationConstants.Queues.DeadLetters,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            IntegrationConstants.Queues.DeadLetters,
            IntegrationConstants.Exchanges.DeadLetter,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        var deadLetterArgs = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = IntegrationConstants.Exchanges.DeadLetter };

        // Supply Chain exchange and inventory queue.
        await channel.ExchangeDeclareAsync(
            IntegrationConstants.Exchanges.SupplyChain,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            IntegrationConstants.Queues.InventorySupplyIntegration,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        foreach (var routingKey in new[]
        {
            IntegrationConstants.Events.ReceiptItemRegistered,
            IntegrationConstants.Events.ReceiptItemConferred,
            IntegrationConstants.Events.SupplierMaterialMappingCreated,
        })
        {
            await channel.QueueBindAsync(
                IntegrationConstants.Queues.InventorySupplyIntegration,
                IntegrationConstants.Exchanges.SupplyChain,
                routingKey,
                cancellationToken: cancellationToken);
        }

        // Production exchange and inventory queue.
        await channel.ExchangeDeclareAsync(
            IntegrationConstants.Exchanges.Production,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            IntegrationConstants.Queues.InventoryProductionIntegration,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        foreach (var routingKey in new[]
        {
            IntegrationConstants.ProductionEvents.StockReservationRequested,
            IntegrationConstants.ProductionEvents.OrderCompleted,
            IntegrationConstants.ProductionEvents.OrderCancelled,
        })
        {
            await channel.QueueBindAsync(
                IntegrationConstants.Queues.InventoryProductionIntegration,
                IntegrationConstants.Exchanges.Production,
                routingKey,
                cancellationToken: cancellationToken);
        }

        // Logistics exchange and inventory queue.
        await channel.ExchangeDeclareAsync(
            IntegrationConstants.Exchanges.Logistics,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            IntegrationConstants.Queues.InventoryLogisticsIntegration,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            IntegrationConstants.Queues.InventoryLogisticsIntegration,
            IntegrationConstants.Exchanges.Logistics,
            IntegrationConstants.LogisticsEvents.ShipmentDispatched,
            cancellationToken: cancellationToken);

        logger.LogInformation("RabbitMQ topology declared successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
