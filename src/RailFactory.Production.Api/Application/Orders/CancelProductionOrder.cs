using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Cancels a Production Order. Blocked if the order is InExecution or Completed.
/// </summary>
public sealed class CancelProductionOrder(IProductionOrderRepository repository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        order.Cancel();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
