using RailFactory.Fleet.Api.Application.Ports;

namespace RailFactory.Fleet.Api.Application.Maintenance;

public sealed class CompleteMaintenance(IMaintenanceRepository maintenance)
{
    public async Task ExecuteAsync(Guid vehicleId, Guid planId, DateOnly completedDate, CancellationToken ct)
    {
        var plan = await maintenance.GetByIdAsync(vehicleId, planId, ct)
            ?? throw new KeyNotFoundException($"Maintenance plan {planId} not found.");

        plan.Complete(completedDate);
        await maintenance.SaveAsync(plan, ct);
    }
}
