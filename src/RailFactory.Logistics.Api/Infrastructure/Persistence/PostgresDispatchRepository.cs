using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresDispatchRepository(LogisticsDbContext db) : IDispatchRepository
{
    public Task<Dispatch?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Dispatches.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Dispatch?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct)
        => db.Dispatches.FirstOrDefaultAsync(x => x.TrackingCode == trackingCode, ct);

    public async Task SaveAsync(Dispatch dispatch, CancellationToken ct)
    {
        if (db.Entry(dispatch).State == EntityState.Detached)
            db.Dispatches.Add(dispatch);
        await db.SaveChangesAsync(ct);
    }
}
