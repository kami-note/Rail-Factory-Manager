using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Telemetry;

public sealed class ListTelemetryEvents(ITelemetryRepository telemetryRepo)
{
    public Task<List<VehicleTelemetryEvent>> ExecuteAsync(Guid vehicleId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
        => telemetryRepo.ListByVehicleIdAsync(vehicleId, from, to, ct);
}
