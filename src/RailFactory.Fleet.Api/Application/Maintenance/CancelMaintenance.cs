using RailFactory.Fleet.Api.Application.Ports;

namespace RailFactory.Fleet.Api.Application.Maintenance;

public sealed class CancelMaintenance(IMaintenanceRepository maintenance)
{
    public async Task ExecuteAsync(Guid vehicleId, Guid planId, CancellationToken ct)
    {
        var plan = await maintenance.GetByIdAsync(vehicleId, planId, ct)
            ?? throw new KeyNotFoundException($"Maintenance plan {planId} not found.");

        plan.Cancel();
        await maintenance.SaveAsync(plan, ct);
    }
}
