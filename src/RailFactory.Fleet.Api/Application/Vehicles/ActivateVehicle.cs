using RailFactory.Fleet.Api.Application.Ports;

namespace RailFactory.Fleet.Api.Application.Vehicles;

public sealed class ActivateVehicle(IVehicleRepository repository)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found.");
        vehicle.Activate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
