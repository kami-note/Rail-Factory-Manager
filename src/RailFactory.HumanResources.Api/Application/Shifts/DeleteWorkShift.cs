using RailFactory.HumanResources.Api.Application.Ports;

namespace RailFactory.HumanResources.Api.Application.Shifts;

public sealed class DeleteWorkShift(IWorkShiftRepository shiftRepo)
{
    public async Task<bool> ExecuteAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await shiftRepo.GetByIdAsync(shiftId, ct);
        if (shift is null) return false;

        await shiftRepo.RemoveAsync(shift, ct);
        await shiftRepo.SaveChangesAsync(ct);
        return true;
    }
}
