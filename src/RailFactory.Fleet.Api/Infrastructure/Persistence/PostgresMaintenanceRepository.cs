using Microsoft.EntityFrameworkCore;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class PostgresMaintenanceRepository(FleetDbContext db) : IMaintenanceRepository
{
    public Task<VehicleMaintenancePlan?> GetByIdAsync(Guid vehicleId, Guid planId, CancellationToken ct)
        => db.MaintenancePlans
            .FirstOrDefaultAsync(x => x.Id == planId && x.VehicleId == vehicleId, ct);

    public Task<List<VehicleMaintenancePlan>> ListByVehicleAsync(Guid vehicleId, CancellationToken ct)
        => db.MaintenancePlans
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.ScheduledDate)
            .ToListAsync(ct);

    public async Task SaveAsync(VehicleMaintenancePlan plan, CancellationToken ct)
    {
        if (db.Entry(plan).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            db.MaintenancePlans.Add(plan);
        await db.SaveChangesAsync(ct);
    }
}
