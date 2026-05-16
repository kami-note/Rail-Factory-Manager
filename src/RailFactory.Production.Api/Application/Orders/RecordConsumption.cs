using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Records the actual quantity of a material consumed during order execution.
/// </summary>
public sealed class RecordConsumption(
    IProductionOrderRepository orderRepository,
    IExecutionRepository executionRepository)
{
    public async Task ExecuteAsync(RecordConsumptionInput input, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(input.ProductionOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{input.ProductionOrderId}' not found.");

        if (order.Status != ProductionOrderStatus.InExecution)
            throw new InvalidOperationException($"Cannot record consumption for order in status '{order.Status}'. Order must be InExecution.");

        var record = ConsumptionRecord.Create(
            input.ProductionOrderId,
            input.MaterialCode,
            input.ConsumedQuantity,
            input.UnitOfMeasure,
            input.InventoryBalanceId);

        await executionRepository.AddConsumptionAsync(record, cancellationToken);
        await executionRepository.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RecordConsumptionInput(
    Guid ProductionOrderId,
    string MaterialCode,
    decimal ConsumedQuantity,
    string UnitOfMeasure,
    Guid? InventoryBalanceId);
