using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Shifts;

public sealed class ListWorkShifts(IWorkShiftRepository shiftRepo)
{
    public Task<List<WorkShift>> ExecuteAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken ct)
        => shiftRepo.ListByPersonIdAsync(personId, from, to, ct);
}
