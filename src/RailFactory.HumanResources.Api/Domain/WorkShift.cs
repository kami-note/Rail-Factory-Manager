namespace RailFactory.HumanResources.Api.Domain;

/// <summary>
/// Represents a scheduled work shift for a person (RD-HR-03).
/// </summary>
public sealed class WorkShift
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly ShiftDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WorkShift() { }

    private WorkShift(Guid id, Guid personId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, string? notes)
    {
        Id = id;
        PersonId = personId;
        ShiftDate = shiftDate;
        StartTime = startTime;
        EndTime = endTime;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static WorkShift Create(Guid personId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, string? notes = null)
    {
        if (endTime <= startTime)
            throw new ArgumentException("EndTime must be after StartTime.");

        return new WorkShift(Guid.NewGuid(), personId, shiftDate, startTime, endTime, notes?.Trim());
    }
}
