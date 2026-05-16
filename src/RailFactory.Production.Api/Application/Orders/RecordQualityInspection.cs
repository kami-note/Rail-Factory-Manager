using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Records a quality inspection result for a Production Order in execution.
/// </summary>
public sealed class RecordQualityInspection(
    IProductionOrderRepository orderRepository,
    IExecutionRepository executionRepository)
{
    public async Task ExecuteAsync(RecordQualityInspectionInput input, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(input.ProductionOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{input.ProductionOrderId}' not found.");

        if (order.Status != ProductionOrderStatus.InExecution)
            throw new InvalidOperationException($"Cannot record inspection for order in status '{order.Status}'. Order must be InExecution.");

        var inspection = QualityInspection.Create(
            input.ProductionOrderId,
            input.Result,
            input.InspectedBy,
            input.Notes);

        await executionRepository.AddInspectionAsync(inspection, cancellationToken);
        await executionRepository.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RecordQualityInspectionInput(
    Guid ProductionOrderId,
    InspectionResult Result,
    string InspectedBy,
    string? Notes);
