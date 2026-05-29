namespace RailFactory.Fleet.Api.Domain;

/// <summary>
/// Represents the assignment of a driver to a vehicle for a time window (RF-28).
/// DriverPersonId is a cross-service reference to a Person in the HR bounded context.
/// </summary>
public sealed class DriverAssignment
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }

    /// <summary>
    /// Reference to a Person (Type=Driver) in RailFactory.HumanResources.
    /// No FK constraint — cross-service reference by convention.
    /// </summary>
    public Guid DriverPersonId { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private DriverAssignment() { }

    private DriverAssignment(Guid id, Guid vehicleId, Guid driverPersonId,
        DateOnly startDate, DateOnly? endDate, string? notes)
    {
        Id = id;
        VehicleId = vehicleId;
        DriverPersonId = driverPersonId;
        StartDate = startDate;
        EndDate = endDate;
        Notes = notes;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    internal static DriverAssignment Create(Guid vehicleId, Guid driverPersonId,
        DateOnly startDate, DateOnly? endDate, string? notes)
    {
        if (endDate.HasValue && endDate.Value < startDate)
            throw new ArgumentException("End date must be after start date.");

        return new DriverAssignment(Guid.NewGuid(), vehicleId, driverPersonId, startDate, endDate, notes?.Trim());
    }
}
