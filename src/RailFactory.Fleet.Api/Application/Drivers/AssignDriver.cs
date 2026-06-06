using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Drivers;

public sealed record AssignDriverInput(
    Guid VehicleId, Guid DriverPersonId,
    DateOnly StartDate, DateOnly? EndDate, string? Notes);

public sealed class AssignDriver(IVehicleRepository repository)
{
    public async Task<DriverAssignment> ExecuteAsync(AssignDriverInput input, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdAsync(input.VehicleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vehicle {input.VehicleId} not found.");

        var assignment = vehicle.AssignDriver(
            input.DriverPersonId, input.StartDate, input.EndDate, input.Notes);

        await repository.AddAssignmentAsync(assignment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return assignment;
    }
}
