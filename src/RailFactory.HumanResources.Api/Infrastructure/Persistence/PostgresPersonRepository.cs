using Microsoft.EntityFrameworkCore;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class PostgresPersonRepository(HrDbContext context) : IPersonRepository
{
    public async Task AddAsync(Person person, CancellationToken cancellationToken)
        => await context.People.AddAsync(person, cancellationToken);

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.People.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Person>> ListAsync(PersonType? type, PersonStatus? status, CancellationToken cancellationToken)
    {
        var query = context.People.AsQueryable();
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
