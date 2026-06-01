using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Shifts;

public sealed class CreateWorkShift(IPersonRepository personRepo, IWorkShiftRepository shiftRepo)
{
    public async Task<WorkShift> ExecuteAsync(CreateWorkShiftInput input, CancellationToken ct)
    {
        var person = await personRepo.GetByIdAsync(input.PersonId, ct)
            ?? throw new KeyNotFoundException($"Person {input.PersonId} not found.");

        if (person.Status == PersonStatus.Inactive)
            throw new InvalidOperationException("Cannot schedule shifts for an inactive person.");

        var shift = WorkShift.Create(input.PersonId, input.ShiftDate, input.StartTime, input.EndTime, input.Notes);
        await shiftRepo.AddAsync(shift, ct);
        await shiftRepo.SaveChangesAsync(ct);
        return shift;
    }
}

public sealed record CreateWorkShiftInput(
    Guid PersonId,
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes);
