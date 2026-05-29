namespace RailFactory.Fleet.Api.Domain;

public enum MaintenanceType { Preventive, Corrective }
public enum MaintenanceStatus { Scheduled, Done, Cancelled }

public sealed class VehicleMaintenancePlan
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public MaintenanceType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateOnly ScheduledDate { get; private set; }
    public DateOnly? CompletedDate { get; private set; }
    public MaintenanceStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VehicleMaintenancePlan() { }

    public static VehicleMaintenancePlan Create(
        Guid vehicleId, MaintenanceType type, string description,
        DateOnly scheduledDate, string? notes)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        return new VehicleMaintenancePlan
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            Type = type,
            Description = description.Trim(),
            ScheduledDate = scheduledDate,
            Status = MaintenanceStatus.Scheduled,
            Notes = notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Complete(DateOnly completedDate)
    {
        if (Status != MaintenanceStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled maintenance can be completed.");
        Status = MaintenanceStatus.Done;
        CompletedDate = completedDate;
    }

    public void Cancel()
    {
        if (Status != MaintenanceStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled maintenance can be cancelled.");
        Status = MaintenanceStatus.Cancelled;
    }
}
