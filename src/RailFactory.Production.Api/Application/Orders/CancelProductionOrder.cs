using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Cancels a Production Order. Blocked if the order is InExecution or Completed.
/// When the order was Released (stock already reserved), enqueues a
/// production_order_cancelled outbox entry so Inventory can release the reservations.
/// </summary>
public sealed class CancelProductionOrder(IProductionOrderRepository repository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        var hadReservations = order.Status == ProductionOrderStatus.Released;

        order.Cancel();

        if (hadReservations)
        {
            var payload = JsonSerializer.Serialize(new
            {
                orderId = order.Id,
                orderNumber = order.OrderNumber
            });
            var outboxMessage = ProductionOutboxMessage.Create(
                IntegrationConstants.ProductionEvents.ProductionOrderCancelled, payload);
            await repository.AddOutboxMessageAsync(outboxMessage, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
