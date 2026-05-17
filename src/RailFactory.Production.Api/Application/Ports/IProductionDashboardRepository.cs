namespace RailFactory.Production.Api.Application.Ports;

public interface IProductionDashboardRepository
{
    Task<Dictionary<string, int>> GetOrderCountsByStatusAsync(CancellationToken ct);
    Task<IReadOnlyList<MaterialScrapSummary>> GetTopScrapByMaterialAsync(int top, CancellationToken ct);
    Task<(int Passed, int Failed)> GetInspectionSummaryAsync(CancellationToken ct);
}

public sealed record MaterialScrapSummary(string MaterialCode, decimal TotalScrap, string UnitOfMeasure);
