using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Ports;

/// <summary>
/// Persistence port for the WorkCenter aggregate.
/// </summary>
public interface IWorkCenterRepository
{
    Task AddAsync(WorkCenter workCenter, CancellationToken cancellationToken);
    Task<WorkCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkCenter?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<List<WorkCenter>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if any Production Order in Released or InExecution status references the given Work Center.
    /// </summary>
    Task<bool> HasActiveOrdersAsync(Guid workCenterId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
