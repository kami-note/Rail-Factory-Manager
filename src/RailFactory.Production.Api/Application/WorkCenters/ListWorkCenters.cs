using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.WorkCenters;

/// <summary>
/// Returns all Work Centers registered in the tenant boundary.
/// </summary>
public sealed class ListWorkCenters(IWorkCenterRepository repository)
{
    public Task<List<WorkCenter>> ExecuteAsync(CancellationToken cancellationToken)
        => repository.ListAsync(cancellationToken);
}
