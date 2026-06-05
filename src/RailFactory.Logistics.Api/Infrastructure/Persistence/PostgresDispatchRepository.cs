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

    public Task<List<Dispatch>> ListAsync(int page, int pageSize, CancellationToken ct)
        => db.Dispatches
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<(List<Dispatch> Items, int Total)> ListFiscalAsync(int page, int pageSize, IReadOnlyList<string> statuses, CancellationToken ct)
    {
        var query = db.Dispatches.AsNoTracking().Where(x => x.FiscalStatus != null);
        if (statuses.Count > 0)
            query = query.Where(x => statuses.Contains(x.FiscalStatus!));
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.DispatchedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task SaveAsync(Dispatch dispatch, CancellationToken ct)
    {
        if (db.Entry(dispatch).State == EntityState.Detached)
            db.Dispatches.Add(dispatch);
        await db.SaveChangesAsync(ct);
    }
}
