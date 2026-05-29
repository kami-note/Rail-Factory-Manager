using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Maintenance;

public sealed record ScheduleMaintenanceInput(
    Guid VehicleId, MaintenanceType Type, string Description,
    DateOnly ScheduledDate, string? Notes);

public sealed class ScheduleMaintenance(IVehicleRepository vehicles, IMaintenanceRepository maintenance)
{
    public async Task<VehicleMaintenancePlan> ExecuteAsync(ScheduleMaintenanceInput input, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(input.VehicleId, ct)
            ?? throw new KeyNotFoundException($"Vehicle {input.VehicleId} not found.");

        var plan = VehicleMaintenancePlan.Create(
            vehicle.Id, input.Type, input.Description, input.ScheduledDate, input.Notes);

        await maintenance.SaveAsync(plan, ct);
        return plan;
    }
}
