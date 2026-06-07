using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Fleet.Api.Domain;

/// <summary>
/// Represents a vehicle in the company's fleet (RF-25, RD-FLE-01).
/// Holds official documents, load capacity and operational status.
/// </summary>
public sealed class Vehicle : AggregateRoot<Guid>
{
    public string Plate { get; private set; }
    public string Chassis { get; private set; }
    public string Renavam { get; private set; }
    /// <summary>Registro Nacional de Transportadores Rodoviários de Carga — required for MDF-e.</summary>
    public string? Rntrc { get; private set; }
    public VehicleType Type { get; private set; }
    public VehicleStatus Status { get; private set; }

    /// <summary>Maximum load weight in kilograms (RD-FLE-01).</summary>
    public decimal MaxWeightKg { get; private set; }

    /// <summary>Maximum load volume in cubic meters (RD-FLE-01).</summary>
    public decimal MaxVolumeCbm { get; private set; }

    /// <summary>CRLV expiry date (RF-25).</summary>
    public DateOnly LicenseExpiry { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<DriverAssignment> _assignments = [];
    public IReadOnlyList<DriverAssignment> Assignments => _assignments.AsReadOnly();

    private Vehicle() : base(Guid.Empty)
    {
        Plate = string.Empty;
        Chassis = string.Empty;
        Renavam = string.Empty;
    }

    private Vehicle(Guid id, string plate, string chassis, string renavam, string? rntrc,
        VehicleType type, decimal maxWeightKg, decimal maxVolumeCbm, DateOnly licenseExpiry) : base(id)
    {
        Plate = plate;
        Chassis = chassis;
        Renavam = renavam;
        Rntrc = rntrc;
        Type = type;
        MaxWeightKg = maxWeightKg;
        MaxVolumeCbm = maxVolumeCbm;
        LicenseExpiry = licenseExpiry;
        Status = VehicleStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Vehicle Create(string plate, string chassis, string renavam, string? rntrc,
        VehicleType type, decimal maxWeightKg, decimal maxVolumeCbm, DateOnly licenseExpiry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plate);
        ArgumentException.ThrowIfNullOrWhiteSpace(chassis);
        ArgumentException.ThrowIfNullOrWhiteSpace(renavam);
        if (maxWeightKg < 0) throw new ArgumentOutOfRangeException(nameof(maxWeightKg));
        if (maxVolumeCbm < 0) throw new ArgumentOutOfRangeException(nameof(maxVolumeCbm));

        return new Vehicle(
            Guid.NewGuid(),
            plate.Trim().ToUpperInvariant(),
            chassis.Trim().ToUpperInvariant(),
            renavam.Trim(),
            string.IsNullOrWhiteSpace(rntrc) ? null : rntrc.Trim(),
            type, maxWeightKg, maxVolumeCbm, licenseExpiry);
    }

    public void SetRntrc(string? rntrc)
    {
        Rntrc = string.IsNullOrWhiteSpace(rntrc) ? null : rntrc.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <exception cref="InvalidOperationException">Already inactive.</exception>
    public void Deactivate()
    {
        if (Status == VehicleStatus.Inactive)
            throw new InvalidOperationException("Vehicle is already inactive.");

        Status = VehicleStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <exception cref="InvalidOperationException">Already active.</exception>
    public void Activate()
    {
        if (Status == VehicleStatus.Active)
            throw new InvalidOperationException("Vehicle is already active.");

        Status = VehicleStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Assigns a driver to this vehicle for a given time window (RF-28).
    /// The driver must be a Person with Type=Driver managed by HR.
    /// </summary>
    public DriverAssignment AssignDriver(Guid driverPersonId, DateOnly startDate, DateOnly? endDate, string? notes)
    {
        if (Status == VehicleStatus.Inactive)
            throw new InvalidOperationException("Cannot assign a driver to an inactive vehicle.");

        var assignment = DriverAssignment.Create(Id, driverPersonId, startDate, endDate, notes);
        _assignments.Add(assignment);
        return assignment;
    }
}

public enum VehicleType
{
    Car        = 0,
    Truck      = 1,
    Van        = 2,
    Motorcycle = 3
}

public enum VehicleStatus
{
    Active   = 0,
    Inactive = 1
}
