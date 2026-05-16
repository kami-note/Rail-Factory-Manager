using Microsoft.EntityFrameworkCore;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class PostgresWorkCenterRepository(ProductionDbContext context) : IWorkCenterRepository
{
    public async Task AddAsync(WorkCenter workCenter, CancellationToken cancellationToken)
        => await context.WorkCenters.AddAsync(workCenter, cancellationToken);

    public Task<WorkCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.WorkCenters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<WorkCenter?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => context.WorkCenters.FirstOrDefaultAsync(x => x.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public Task<List<WorkCenter>> ListAsync(CancellationToken cancellationToken)
        => context.WorkCenters.OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public Task<bool> HasActiveOrdersAsync(Guid workCenterId, CancellationToken cancellationToken)
        => context.ProductionOrders.AnyAsync(
            x => x.WorkCenterId == workCenterId &&
                 (x.Status == ProductionOrderStatus.Released || x.Status == ProductionOrderStatus.InExecution),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
