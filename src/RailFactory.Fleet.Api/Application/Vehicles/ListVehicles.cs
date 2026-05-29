using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Vehicles;

public sealed class ListVehicles(IVehicleRepository repository)
{
    public Task<List<Vehicle>> ExecuteAsync(VehicleStatus? status, CancellationToken cancellationToken)
        => repository.ListAsync(status, cancellationToken);
}
