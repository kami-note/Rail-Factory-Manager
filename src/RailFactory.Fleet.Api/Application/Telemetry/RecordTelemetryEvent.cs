using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Telemetry;

public sealed class RecordTelemetryEvent(IVehicleRepository vehicleRepo, ITelemetryRepository telemetryRepo)
{
    public async Task<VehicleTelemetryEvent> ExecuteAsync(RecordTelemetryInput input, CancellationToken ct)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(input.VehicleId, ct)
            ?? throw new KeyNotFoundException($"Vehicle {input.VehicleId} not found.");

        if (vehicle.Status == VehicleStatus.Inactive)
            throw new InvalidOperationException("Cannot record telemetry for an inactive vehicle.");

        var ev = VehicleTelemetryEvent.Create(
            input.VehicleId, input.DriverPersonId, input.EventType,
            input.Description, input.OccurredAt, input.LatitudeDeg, input.LongitudeDeg);

        await telemetryRepo.AddAsync(ev, ct);
        await telemetryRepo.SaveChangesAsync(ct);
        return ev;
    }
}

public sealed record RecordTelemetryInput(
    Guid VehicleId,
    Guid? DriverPersonId,
    string EventType,
    string Description,
    DateTimeOffset OccurredAt,
    decimal? LatitudeDeg,
    decimal? LongitudeDeg);
