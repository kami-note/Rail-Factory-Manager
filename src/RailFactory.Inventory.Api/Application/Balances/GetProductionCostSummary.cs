using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Aggregates production material consumption for the cost dashboard (RF-38).
/// Returns material consumption totals. Unit cost integration is external (Supply Chain NF-e prices).
/// </summary>
public sealed class GetProductionCostSummary(IInventoryRepository repository)
{
    public Task<List<MaterialConsumptionSummary>> ExecuteAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
        => repository.GetProductionCostSummaryAsync(from, to, ct);
}
