using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.WorkCenters;

/// <summary>
/// Deactivates a Work Center, guarded by the presence of active Production Orders.
/// </summary>
public sealed class DeactivateWorkCenter(IWorkCenterRepository repository)
{
    public async Task ExecuteAsync(Guid workCenterId, CancellationToken cancellationToken)
    {
        var workCenter = await repository.GetByIdAsync(workCenterId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work Center '{workCenterId}' not found.");

        var hasActiveOrders = await repository.HasActiveOrdersAsync(workCenterId, cancellationToken);
        if (hasActiveOrders)
            throw new InvalidOperationException("Cannot deactivate a Work Center with Released or In-Execution Production Orders.");

        workCenter.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
