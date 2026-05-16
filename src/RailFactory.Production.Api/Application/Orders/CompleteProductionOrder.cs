using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Completes a Production Order after verifying a passed quality inspection exists.
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
        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
