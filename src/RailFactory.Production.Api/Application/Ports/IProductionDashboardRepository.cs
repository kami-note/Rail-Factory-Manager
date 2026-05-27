namespace RailFactory.Production.Api.Application.Ports;

public interface IProductionDashboardRepository
{
    Task<Dictionary<string, int>> GetOrderCountsByStatusAsync(CancellationToken ct);
    Task<IReadOnlyList<MaterialScrapSummary>> GetTopScrapByMaterialAsync(int top, CancellationToken ct);
    Task<(int Passed, int Failed)> GetInspectionSummaryAsync(CancellationToken ct);

    /// <summary>
    /// Returns the average lead time in hours for Completed production orders,
    /// measured from CreatedAt to completion (UpdatedAt). Returns null when no completed orders exist.
    /// </summary>
    Task<double?> GetAverageLeadTimeHoursAsync(CancellationToken ct);

    /// <summary>
    /// Returns per-Work-Center order totals and completion rate for the dashboard.
    /// </summary>
    Task<IReadOnlyList<WorkCenterOrderSummary>> GetWorkCenterOrderSummaryAsync(CancellationToken ct);
}

public sealed record MaterialScrapSummary(string MaterialCode, decimal TotalScrap, string UnitOfMeasure);

public sealed record WorkCenterOrderSummary(
    Guid WorkCenterId,
    string WorkCenterCode,
    string WorkCenterName,
    int TotalOrders,
    int CompletedOrders,
    double CompletionRate);
