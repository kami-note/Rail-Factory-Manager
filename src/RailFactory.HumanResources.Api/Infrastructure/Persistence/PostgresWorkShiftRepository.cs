using Microsoft.EntityFrameworkCore;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class PostgresWorkShiftRepository(HrDbContext dbContext) : IWorkShiftRepository
{
    public Task<List<WorkShift>> ListByPersonIdAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = dbContext.WorkShifts.Where(x => x.PersonId == personId);
        if (from.HasValue) query = query.Where(x => x.ShiftDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.ShiftDate <= to.Value);
        return query.OrderBy(x => x.ShiftDate).ThenBy(x => x.StartTime).ToListAsync(ct);
    }

    public Task<WorkShift?> GetByIdAsync(Guid id, CancellationToken ct)
        => dbContext.WorkShifts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(WorkShift shift, CancellationToken ct)
        => dbContext.WorkShifts.AddAsync(shift, ct).AsTask();

    public Task RemoveAsync(WorkShift shift, CancellationToken ct)
    {
        dbContext.WorkShifts.Remove(shift);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => dbContext.SaveChangesAsync(ct);
}
