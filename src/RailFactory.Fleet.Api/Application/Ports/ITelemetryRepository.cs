using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Ports;

public interface ITelemetryRepository
{
    Task<List<VehicleTelemetryEvent>> ListByVehicleIdAsync(Guid vehicleId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
    Task AddAsync(VehicleTelemetryEvent telemetryEvent, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
