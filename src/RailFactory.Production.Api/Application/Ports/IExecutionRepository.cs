using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Ports;

/// <summary>
/// Persistence port for execution-related aggregates: consumption, scrap and quality inspection.
/// </summary>
public interface IExecutionRepository
{
    Task AddConsumptionAsync(ConsumptionRecord record, CancellationToken cancellationToken);
    Task AddScrapAsync(ScrapRecord record, CancellationToken cancellationToken);
    Task AddInspectionAsync(QualityInspection inspection, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the latest inspection for an order, or null if none exists.
    /// </summary>
    Task<QualityInspection?> GetLatestInspectionAsync(Guid productionOrderId, CancellationToken cancellationToken);

    Task<List<QualityInspection>> GetInspectionsByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken);
    Task<List<ConsumptionRecord>> GetConsumptionByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken);
    Task<List<ScrapRecord>> GetScrapByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
