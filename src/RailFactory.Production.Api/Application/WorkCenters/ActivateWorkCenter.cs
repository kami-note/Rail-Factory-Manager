using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.WorkCenters;

/// <summary>
/// Reactivates a previously deactivated Work Center.
/// </summary>
public sealed class ActivateWorkCenter(IWorkCenterRepository repository)
{
    public async Task ExecuteAsync(Guid workCenterId, CancellationToken cancellationToken)
    {
        var workCenter = await repository.GetByIdAsync(workCenterId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work Center '{workCenterId}' not found.");

        workCenter.Activate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
