using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Balances;

public sealed class GetInventoryDashboard(IInventoryRepository repo)
{
    public async Task<InventoryDashboardResult> ExecuteAsync(CancellationToken ct)
    {
        var summary = await repo.GetStockSummaryAsync(ct);

        // Inventory Accuracy = Available / (Available + Blocked).
        // Blocked balances represent conferred receipts with quantity divergences.
        // Pending and Reserved balances are excluded — they have not yet completed the conference cycle.
        var conferencedTotal = summary.AvailableCount + summary.BlockedCount;
        double? accuracy = conferencedTotal > 0
            ? Math.Round((double)summary.AvailableCount / conferencedTotal, 4)
            : null;

        return new InventoryDashboardResult(
            summary.TotalMaterials,
            summary.MaterialsWithStock,
            summary.AvailableCount,
            summary.ReservedCount,
            summary.BlockedCount,
            accuracy);
    }
}

public sealed record InventoryDashboardResult(
    int TotalMaterials,
    int MaterialsWithStock,
    int AvailableCount,
    int ReservedCount,
    int BlockedCount,
    /// <summary>
    /// Ratio of Available to (Available + Blocked) conferred balances.
    /// Null when no balance has completed the conference cycle.
    /// </summary>
    double? StockAccuracy);
