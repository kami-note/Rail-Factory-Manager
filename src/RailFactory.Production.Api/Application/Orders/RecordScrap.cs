using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Records material scrapped during order execution, with a mandatory reason.
/// </summary>
public sealed class RecordScrap(
    IProductionOrderRepository orderRepository,
    IExecutionRepository executionRepository)
{
    public async Task ExecuteAsync(RecordScrapInput input, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(input.ProductionOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{input.ProductionOrderId}' not found.");

        if (order.Status != ProductionOrderStatus.InExecution)
            throw new InvalidOperationException($"Cannot record scrap for order in status '{order.Status}'. Order must be InExecution.");

        var record = ScrapRecord.Create(
            input.ProductionOrderId,
            input.MaterialCode,
            input.ScrapQuantity,
            input.UnitOfMeasure,
            input.Reason);

        await executionRepository.AddScrapAsync(record, cancellationToken);
        await executionRepository.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RecordScrapInput(
    Guid ProductionOrderId,
    string MaterialCode,
    decimal ScrapQuantity,
    string UnitOfMeasure,
    string Reason);
