using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.HumanResources.Api.Domain;

/// <summary>
/// Represents a person registered in the system — employee, driver, or external contractor.
/// Persons without system access (RF-31) are managed here.
/// </summary>
public sealed class Person : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string DocumentNumber { get; private set; }
    public PersonType Type { get; private set; }
    public PersonStatus Status { get; private set; }
    public string? Email { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Person() : base(Guid.Empty)
    {
        Name = string.Empty;
        DocumentNumber = string.Empty;
    }

    private Person(Guid id, string name, string documentNumber, PersonType type, string? email) : base(id)
    {
        Name = name;
        DocumentNumber = documentNumber;
        Type = type;
        Email = email;
        Status = PersonStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to register a new person.
    /// </summary>
    public static Person Create(string name, string documentNumber, PersonType type, string? email = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);

        return new Person(Guid.NewGuid(), name.Trim(), documentNumber.Trim(), type, email?.Trim().ToLowerInvariant());
    }

    /// <exception cref="InvalidOperationException">Already inactive.</exception>
    public void Deactivate()
    {
        if (Status == PersonStatus.Inactive)
            throw new InvalidOperationException("Person is already inactive.");

        Status = PersonStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <exception cref="InvalidOperationException">Already active.</exception>
    public void Activate()
    {
        if (Status == PersonStatus.Active)
            throw new InvalidOperationException("Person is already active.");

        Status = PersonStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum PersonType
{
    Employee   = 0,
    Driver     = 1,
    Contractor = 2
}

public enum PersonStatus
{
    Active   = 0,
    Inactive = 1
}
