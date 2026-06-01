using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Ports;

public interface IWorkShiftRepository
{
    Task<List<WorkShift>> ListByPersonIdAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<WorkShift?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(WorkShift shift, CancellationToken ct);
    Task RemoveAsync(WorkShift shift, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
