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
}
