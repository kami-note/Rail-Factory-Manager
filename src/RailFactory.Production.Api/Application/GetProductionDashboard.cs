using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application;

public sealed class GetProductionDashboard(IProductionDashboardRepository repo)
{
    public async Task<ProductionDashboardResult> ExecuteAsync(CancellationToken ct)
    {
        var ordersByStatus = await repo.GetOrderCountsByStatusAsync(ct);
        var topScrap = await repo.GetTopScrapByMaterialAsync(5, ct);
        var (passed, failed) = await repo.GetInspectionSummaryAsync(ct);
        var avgLeadTimeHours = await repo.GetAverageLeadTimeHoursAsync(ct);
        var workCenterSummary = await repo.GetWorkCenterOrderSummaryAsync(ct);

        var total = passed + failed;
        var passRate = total > 0 ? Math.Round((double)passed / total, 3) : 0d;
        var activeOrders =
            ordersByStatus.GetValueOrDefault("Released") +
            ordersByStatus.GetValueOrDefault("InExecution");

        return new ProductionDashboardResult(
            ordersByStatus,
            activeOrders,
            topScrap,
            new InspectionSummaryResult(passed, failed, passRate),
            avgLeadTimeHours.HasValue ? Math.Round(avgLeadTimeHours.Value, 1) : null,
            workCenterSummary);
    }
}

public sealed record ProductionDashboardResult(
    Dictionary<string, int> OrdersByStatus,
    int ActiveOrders,
    IReadOnlyList<MaterialScrapSummary> TopScrap,
    InspectionSummaryResult InspectionSummary,
    /// <summary>Average lead time in hours for Completed orders. Null when no completed orders exist.</summary>
    double? AverageLeadTimeHours,
    IReadOnlyList<WorkCenterOrderSummary> WorkCenterSummary);

public sealed record InspectionSummaryResult(int Passed, int Failed, double PassRate);
