namespace RailFactory.Fleet.Api.Domain;

/// <summary>
/// Records a telemetry occurrence for a vehicle or driver (RF-30).
/// </summary>
public sealed class VehicleTelemetryEvent
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid? DriverPersonId { get; private set; }

    /// <summary>
    /// Event classification: speeding, harsh_braking, harsh_acceleration,
    /// idle_excess, accident, breakdown, route_deviation, other.
    /// </summary>
    public string EventType { get; private set; }

    public string Description { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public decimal? LatitudeDeg { get; private set; }
    public decimal? LongitudeDeg { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VehicleTelemetryEvent()
    {
        EventType = string.Empty;
        Description = string.Empty;
    }

    private VehicleTelemetryEvent(Guid id, Guid vehicleId, Guid? driverPersonId, string eventType,
        string description, DateTimeOffset occurredAt, decimal? lat, decimal? lon)
    {
        Id = id;
        VehicleId = vehicleId;
        DriverPersonId = driverPersonId;
        EventType = eventType;
        Description = description;
        OccurredAt = occurredAt;
        LatitudeDeg = lat;
        LongitudeDeg = lon;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static VehicleTelemetryEvent Create(Guid vehicleId, Guid? driverPersonId, string eventType,
        string description, DateTimeOffset occurredAt, decimal? latitudeDeg = null, decimal? longitudeDeg = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new VehicleTelemetryEvent(Guid.NewGuid(), vehicleId, driverPersonId,
            eventType.Trim().ToLowerInvariant(), description.Trim(), occurredAt, latitudeDeg, longitudeDeg);
    }
}
