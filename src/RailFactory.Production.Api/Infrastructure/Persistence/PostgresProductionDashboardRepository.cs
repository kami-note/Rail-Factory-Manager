using Microsoft.EntityFrameworkCore;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class PostgresProductionDashboardRepository(ProductionDbContext db) : IProductionDashboardRepository
{
    public async Task<Dictionary<string, int>> GetOrderCountsByStatusAsync(CancellationToken ct)
    {
        var statuses = await db.ProductionOrders
            .Select(x => x.Status)
            .ToListAsync(ct);

        var counts = Enum.GetValues<ProductionOrderStatus>()
            .ToDictionary(s => s.ToString(), _ => 0);

        foreach (var s in statuses)
            counts[s.ToString()]++;

        return counts;
    }

    public async Task<IReadOnlyList<MaterialScrapSummary>> GetTopScrapByMaterialAsync(int top, CancellationToken ct)
    {
        var all = await db.ScrapRecords.ToListAsync(ct);

        return all
            .GroupBy(x => x.MaterialCode.Value)
            .Select(g => new MaterialScrapSummary(
                g.Key,
                g.Sum(s => s.ScrapQuantity),
                g.First().UnitOfMeasure))
            .OrderByDescending(x => x.TotalScrap)
            .Take(top)
            .ToList();
    }

    public async Task<(int Passed, int Failed)> GetInspectionSummaryAsync(CancellationToken ct)
    {
        var results = await db.QualityInspections
            .Select(x => x.Result)
            .ToListAsync(ct);

        var passed = results.Count(r => r == InspectionResult.Passed);
        var failed = results.Count(r => r == InspectionResult.Failed);

        return (passed, failed);
    }

    /// <inheritdoc />
    public async Task<double?> GetAverageLeadTimeHoursAsync(CancellationToken ct)
    {
        // For Completed orders, UpdatedAt reflects the completion timestamp (set by Complete()).
        // Lead time = completion time − creation time.
        var completed = await db.ProductionOrders
            .Where(x => x.Status == ProductionOrderStatus.Completed)
            .Select(x => new { x.CreatedAt, x.UpdatedAt })
            .ToListAsync(ct);

        if (completed.Count == 0)
            return null;

        return completed.Average(x => (x.UpdatedAt - x.CreatedAt).TotalHours);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkCenterOrderSummary>> GetWorkCenterOrderSummaryAsync(CancellationToken ct)
    {
        // Load work centers and orders separately — no EF Core navigation needed.
        var workCenters = await db.WorkCenters
            .Select(x => new { x.Id, x.Code, x.Name })
            .ToListAsync(ct);

        var orders = await db.ProductionOrders
            .Select(x => new { x.WorkCenterId, x.Status })
            .ToListAsync(ct);

        var ordersByCenter = orders
            .GroupBy(x => x.WorkCenterId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Total: g.Count(),
                    Completed: g.Count(o => o.Status == ProductionOrderStatus.Completed)
                ));

        return workCenters
            .Where(wc => ordersByCenter.ContainsKey(wc.Id))
            .Select(wc =>
            {
                var (total, completed) = ordersByCenter[wc.Id];
                var rate = total > 0 ? Math.Round((double)completed / total, 3) : 0d;
                return new WorkCenterOrderSummary(wc.Id, wc.Code, wc.Name, total, completed, rate);
            })
            .OrderByDescending(x => x.TotalOrders)
            .ToList();
    }
}
