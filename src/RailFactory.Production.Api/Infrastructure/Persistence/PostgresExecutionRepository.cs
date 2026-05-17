using Microsoft.EntityFrameworkCore;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class PostgresExecutionRepository(ProductionDbContext context) : IExecutionRepository
{
    public async Task AddConsumptionAsync(ConsumptionRecord record, CancellationToken cancellationToken)
        => await context.ConsumptionRecords.AddAsync(record, cancellationToken);

    public async Task AddScrapAsync(ScrapRecord record, CancellationToken cancellationToken)
        => await context.ScrapRecords.AddAsync(record, cancellationToken);

    public async Task AddInspectionAsync(QualityInspection inspection, CancellationToken cancellationToken)
        => await context.QualityInspections.AddAsync(inspection, cancellationToken);

    public Task<QualityInspection?> GetLatestInspectionAsync(Guid productionOrderId, CancellationToken cancellationToken)
        => context.QualityInspections
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderByDescending(x => x.InspectedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<QualityInspection>> GetInspectionsByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken)
        => context.QualityInspections
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderByDescending(x => x.InspectedAt)
            .ToListAsync(cancellationToken);

    public Task<List<ConsumptionRecord>> GetConsumptionByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken)
        => context.ConsumptionRecords
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);

    public Task<List<ScrapRecord>> GetScrapByOrderAsync(Guid productionOrderId, CancellationToken cancellationToken)
        => context.ScrapRecords
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
