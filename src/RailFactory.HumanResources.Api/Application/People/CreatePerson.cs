using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.People;

public sealed record CreatePersonInput(string Name, string DocumentNumber, string Type, string? Email);

public sealed class CreatePerson(IPersonRepository repository)
{
    public async Task<Person> ExecuteAsync(CreatePersonInput input, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PersonType>(input.Type, ignoreCase: true, out var type))
            throw new ArgumentException($"Invalid person type '{input.Type}'. Valid: {string.Join(", ", Enum.GetNames<PersonType>())}");

        var person = Person.Create(input.Name, input.DocumentNumber, type, input.Email);
        await repository.AddAsync(person, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return person;
    }
}
