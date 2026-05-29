using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Ports;

public interface IMaintenanceRepository
{
    Task<VehicleMaintenancePlan?> GetByIdAsync(Guid vehicleId, Guid planId, CancellationToken ct);
    Task<List<VehicleMaintenancePlan>> ListByVehicleAsync(Guid vehicleId, CancellationToken ct);
    Task SaveAsync(VehicleMaintenancePlan plan, CancellationToken ct);
}
