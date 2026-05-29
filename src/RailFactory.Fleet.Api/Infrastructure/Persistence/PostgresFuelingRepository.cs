using Microsoft.EntityFrameworkCore;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class PostgresFuelingRepository(FleetDbContext db) : IFuelingRepository
{
    public Task<List<FuelingRecord>> ListByVehicleAsync(Guid vehicleId, CancellationToken ct)
        => db.FuelingRecords
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);

    public async Task SaveAsync(FuelingRecord record, CancellationToken ct)
    {
        if (db.Entry(record).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            db.FuelingRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }
}
