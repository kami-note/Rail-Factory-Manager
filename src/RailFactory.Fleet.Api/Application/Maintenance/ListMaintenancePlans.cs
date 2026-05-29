using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Maintenance;

public sealed class ListMaintenancePlans(IMaintenanceRepository maintenance)
{
    public Task<List<VehicleMaintenancePlan>> ExecuteAsync(Guid vehicleId, CancellationToken ct)
        => maintenance.ListByVehicleAsync(vehicleId, ct);
}
