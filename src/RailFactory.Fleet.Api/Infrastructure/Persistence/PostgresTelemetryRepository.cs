using Microsoft.EntityFrameworkCore;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class PostgresTelemetryRepository(FleetDbContext dbContext) : ITelemetryRepository
{
    public Task<List<VehicleTelemetryEvent>> ListByVehicleIdAsync(Guid vehicleId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var query = dbContext.TelemetryEvents.Where(x => x.VehicleId == vehicleId);
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt <= to.Value);
        return query.OrderByDescending(x => x.OccurredAt).ToListAsync(ct);
    }

    public Task AddAsync(VehicleTelemetryEvent telemetryEvent, CancellationToken ct)
        => dbContext.TelemetryEvents.AddAsync(telemetryEvent, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => dbContext.SaveChangesAsync(ct);
}
