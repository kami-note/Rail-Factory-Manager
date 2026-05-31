using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Completes a Production Order after verifying a passed quality inspection exists.
/// Enqueues a production_order_completed outbox entry so Inventory can convert
/// reserved stock into consumed (ledger: production_consumed).
/// </summary>
public sealed class CompleteProductionOrder(
    IProductionOrderRepository orderRepository,
    IExecutionRepository executionRepository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        var latestInspection = await executionRepository.GetLatestInspectionAsync(orderId, cancellationToken);
        var inspectionPassed = latestInspection?.Result == InspectionResult.Passed;

        order.Complete(inspectionPassed);

        var payload = JsonSerializer.Serialize(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber
        });
        var outboxMessage = ProductionOutboxMessage.Create(
            IntegrationConstants.ProductionEvents.ProductionOrderCompleted, payload);
        await orderRepository.AddOutboxMessageAsync(outboxMessage, cancellationToken);

        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
