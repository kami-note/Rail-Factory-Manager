using RailFactory.HumanResources.Api.Application.Ports;

namespace RailFactory.HumanResources.Api.Application.People;

public sealed class ActivatePerson(IPersonRepository repository)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {id} not found.");

        person.Activate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
