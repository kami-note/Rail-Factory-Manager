using Microsoft.EntityFrameworkCore;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class PostgresHourLogRepository(HrDbContext context) : IHourLogRepository
{
    public async Task AddAsync(HourLog hourLog, CancellationToken cancellationToken)
        => await context.HourLogs.AddAsync(hourLog, cancellationToken);

    public Task<List<HourLog>> ListByPersonAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var query = context.HourLogs.Where(x => x.PersonId == personId);
        if (from.HasValue) query = query.Where(x => x.Date >= from.Value);
        if (to.HasValue)   query = query.Where(x => x.Date <= to.Value);
        return query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
