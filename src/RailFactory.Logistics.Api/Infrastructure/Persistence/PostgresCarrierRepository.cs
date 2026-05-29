using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresCarrierRepository(LogisticsDbContext db) : ICarrierRepository
{
    public Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Carriers.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<Carrier>> ListAsync(CarrierStatus? status, CancellationToken ct)
    {
        var query = db.Carriers.AsQueryable();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        return query.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task SaveAsync(Carrier carrier, CancellationToken ct)
    {
        if (db.Entry(carrier).State == EntityState.Detached)
            db.Carriers.Add(carrier);
        await db.SaveChangesAsync(ct);
    }
}
