using Microsoft.EntityFrameworkCore;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class PostgresProductionOrderRepository(ProductionDbContext context) : IProductionOrderRepository
{
    public async Task AddAsync(ProductionOrder order, CancellationToken cancellationToken)
        => await context.ProductionOrders.AddAsync(order, cancellationToken);

    public Task<ProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<ProductionOrder>> ListAsync(
        ProductionOrderStatus? status,
        Guid? workCenterId,
        CancellationToken cancellationToken)
    {
        var query = context.ProductionOrders.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (workCenterId.HasValue)
            query = query.Where(x => x.WorkCenterId == workCenterId.Value);

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddOutboxMessageAsync(ProductionOutboxMessage message, CancellationToken cancellationToken)
        => await context.OutboxMessages.AddAsync(message, cancellationToken);

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"OP-{year}-";

        var count = await context.ProductionOrders
            .CountAsync(x => x.OrderNumber.StartsWith(prefix), cancellationToken);

        return $"{prefix}{(count + 1):D4}";
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
