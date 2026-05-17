using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application;

/// <summary>
/// Returns all execution records (consumptions, scraps, inspections) for a production order.
/// </summary>
public sealed class GetOrderExecutionHistory(
    IProductionOrderRepository orderRepository,
    IExecutionRepository executionRepository)
{
    public async Task<OrderExecutionHistoryResult?> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return null;

        var consumptions = await executionRepository.GetConsumptionByOrderAsync(orderId, cancellationToken);
        var scraps = await executionRepository.GetScrapByOrderAsync(orderId, cancellationToken);
        var inspections = await executionRepository.GetInspectionsByOrderAsync(orderId, cancellationToken);

        return new OrderExecutionHistoryResult(
            consumptions.Select(c => new ConsumptionRecordDto(
                c.MaterialCode.Value,
                c.ConsumedQuantity,
                c.UnitOfMeasure,
                c.RecordedAt)).ToList(),
            scraps.Select(s => new ScrapRecordDto(
                s.MaterialCode.Value,
                s.ScrapQuantity,
                s.UnitOfMeasure,
                s.Reason,
                s.RecordedAt)).ToList(),
            inspections.Select(i => new InspectionRecordDto(
                i.Result.ToString(),
                i.InspectedBy,
                i.Notes,
                i.InspectedAt)).ToList());
    }
}

public sealed record OrderExecutionHistoryResult(
    IReadOnlyList<ConsumptionRecordDto> Consumptions,
    IReadOnlyList<ScrapRecordDto> Scraps,
    IReadOnlyList<InspectionRecordDto> Inspections);

public sealed record ConsumptionRecordDto(
    string MaterialCode,
    decimal ConsumedQuantity,
    string UnitOfMeasure,
    DateTimeOffset RecordedAt);

public sealed record ScrapRecordDto(
    string MaterialCode,
    decimal ScrapQuantity,
    string UnitOfMeasure,
    string Reason,
    DateTimeOffset RecordedAt);

public sealed record InspectionRecordDto(
    string Result,
    string InspectedBy,
    string? Notes,
    DateTimeOffset InspectedAt);
