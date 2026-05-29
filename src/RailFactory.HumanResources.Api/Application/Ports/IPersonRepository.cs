using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Ports;

public interface IPersonRepository
{
    Task AddAsync(Person person, CancellationToken cancellationToken);
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Person>> ListAsync(PersonType? type, PersonStatus? status, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
