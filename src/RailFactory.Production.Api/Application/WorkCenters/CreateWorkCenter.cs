using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.WorkCenters;

/// <summary>
/// Creates a new active Work Center, enforcing code uniqueness within the tenant boundary.
/// </summary>
public sealed class CreateWorkCenter(IWorkCenterRepository repository)
{
    public async Task<WorkCenter> ExecuteAsync(CreateWorkCenterInput input, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(input.Code, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A Work Center with code '{input.Code.Trim().ToUpperInvariant()}' already exists.");

        var workCenter = WorkCenter.Create(input.Code, input.Name);

        await repository.AddAsync(workCenter, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return workCenter;
    }
}

public sealed record CreateWorkCenterInput(string Code, string Name);
