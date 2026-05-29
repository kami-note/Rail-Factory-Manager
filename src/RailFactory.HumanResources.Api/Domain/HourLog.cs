namespace RailFactory.HumanResources.Api.Domain;

/// <summary>
/// Records hours worked by a person on a given day (RD-HR-01).
/// Owned by the person but stored in its own table for query flexibility.
/// </summary>
public sealed class HourLog
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal HoursWorked { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private HourLog() { }

    private HourLog(Guid id, Guid personId, DateOnly date, decimal hoursWorked, string? description)
    {
        Id = id;
        PersonId = personId;
        Date = date;
        HoursWorked = hoursWorked;
        Description = description;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    public static HourLog Create(Guid personId, DateOnly date, decimal hoursWorked, string? description = null)
    {
        if (hoursWorked <= 0 || hoursWorked > 24)
            throw new ArgumentOutOfRangeException(nameof(hoursWorked), "Hours worked must be between 0 and 24.");

        return new HourLog(Guid.NewGuid(), personId, date, hoursWorked, description?.Trim());
    }
}
