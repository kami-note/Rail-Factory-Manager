namespace RailFactory.Logistics.Api.Domain;

public enum DispatchStatus { Pending, InTransit, Delivered, Returned }

public sealed class Dispatch
{
    public Guid Id { get; private set; }
    public Guid ShipmentOrderId { get; private set; }
    public Guid CarrierId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? DriverPersonId { get; private set; }
    public string TrackingCode { get; private set; } = string.Empty;
    public decimal FreightValueBrl { get; private set; }
    public DispatchStatus Status { get; private set; }
    public DateTimeOffset? ConferencedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Dispatch() { }

    public static Dispatch Create(Guid shipmentOrderId, Guid carrierId,
        Guid? vehicleId, Guid? driverPersonId, decimal freightValueBrl)
    {
        var trackingCode = $"RF-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        return new Dispatch
        {
            Id = Guid.NewGuid(),
            ShipmentOrderId = shipmentOrderId,
            CarrierId = carrierId,
            VehicleId = vehicleId,
            DriverPersonId = driverPersonId,
            TrackingCode = trackingCode,
            FreightValueBrl = freightValueBrl,
            Status = DispatchStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Conference()
    {
        if (Status != DispatchStatus.Pending)
            throw new InvalidOperationException("Only Pending dispatches can be conferenced.");
        ConferencedAt = DateTimeOffset.UtcNow;
    }

    public void Ship()
    {
        if (Status != DispatchStatus.Pending)
            throw new InvalidOperationException("Only Pending dispatches can be shipped.");
        Status = DispatchStatus.InTransit;
        DispatchedAt = DateTimeOffset.UtcNow;
    }

    public void Deliver()
    {
        if (Status != DispatchStatus.InTransit)
            throw new InvalidOperationException("Only InTransit dispatches can be delivered.");
        Status = DispatchStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void Return()
    {
        if (Status != DispatchStatus.InTransit)
            throw new InvalidOperationException("Only InTransit dispatches can be returned.");
        Status = DispatchStatus.Returned;
    }
}
