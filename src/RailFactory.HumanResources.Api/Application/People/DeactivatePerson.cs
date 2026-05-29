using RailFactory.HumanResources.Api.Application.Ports;

namespace RailFactory.HumanResources.Api.Application.People;

public sealed class DeactivatePerson(IPersonRepository repository)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {id} not found.");

        person.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
