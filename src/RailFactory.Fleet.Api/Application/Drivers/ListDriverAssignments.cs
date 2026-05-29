using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Drivers;

public sealed class ListDriverAssignments(IVehicleRepository repository)
{
    public async Task<List<DriverAssignment>> ExecuteAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdWithAssignmentsAsync(vehicleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vehicle {vehicleId} not found.");

        return vehicle.Assignments.OrderByDescending(a => a.StartDate).ToList();
    }
}
