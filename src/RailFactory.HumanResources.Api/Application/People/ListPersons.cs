using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.People;

public sealed class ListPersons(IPersonRepository repository)
{
    public Task<List<Person>> ExecuteAsync(PersonType? type, PersonStatus? status, CancellationToken cancellationToken)
        => repository.ListAsync(type, status, cancellationToken);
}
