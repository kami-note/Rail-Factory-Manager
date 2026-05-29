using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Ports;

public interface IVehicleRepository
{
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Vehicle?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Vehicle>> ListAsync(VehicleStatus? status, CancellationToken cancellationToken);
    Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
