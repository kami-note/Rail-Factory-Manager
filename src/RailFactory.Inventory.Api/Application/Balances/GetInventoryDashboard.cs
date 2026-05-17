using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Balances;

public sealed class GetInventoryDashboard(IInventoryRepository repo)
{
    public async Task<InventoryDashboardResult> ExecuteAsync(CancellationToken ct)
    {
        var summary = await repo.GetStockSummaryAsync(ct);

        return new InventoryDashboardResult(
            summary.TotalMaterials,
            summary.MaterialsWithStock,
            summary.AvailableCount,
            summary.ReservedCount);
    }
}

public sealed record InventoryDashboardResult(
    int TotalMaterials,
    int MaterialsWithStock,
    int AvailableCount,
    int ReservedCount);
