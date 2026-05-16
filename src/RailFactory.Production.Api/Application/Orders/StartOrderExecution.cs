using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Transitions a Released Production Order to InExecution when work begins on the shop floor.
/// </summary>
public sealed class StartOrderExecution(IProductionOrderRepository repository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        order.StartExecution();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
