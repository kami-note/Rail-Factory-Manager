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

    // Fiscal document (NF-e) fields — populated after emission via adapter
    public string? FiscalExternalId { get; private set; }
    public string? FiscalAccessKey { get; private set; }
    public string? FiscalStatus { get; private set; }
    public string? FiscalErrorMessage { get; private set; }

    // MDF-e fields — populated after NF-e is authorized
    public string? MdfeExternalId { get; private set; }
    public string? MdfeAccessKey { get; private set; }
    public string? MdfeStatus { get; private set; }
    public string? MdfeErrorMessage { get; private set; }

    // Shipping label (Melhor Envio / Intelipost) — populated by LogisticsShippingDispatcher
    public string? ShippingExternalId { get; private set; }
    public string? ShippingStatus { get; private set; }
    public string? ShippingLabelUrl { get; private set; }
    public string? ShippingTrackingCode { get; private set; }
    public string? ShippingErrorMessage { get; private set; }

    // Vehicle/driver snapshot stored at creation for MDF-e (cross-service data)
    public string? VehiclePlate { get; private set; }
    public string? VehicleRntrc { get; private set; }
    public string? DriverCpf { get; private set; }
    public string? DriverName { get; private set; }

    private Dispatch() { }

    public static Dispatch Create(Guid shipmentOrderId, Guid carrierId,
        Guid? vehicleId, Guid? driverPersonId, decimal freightValueBrl,
        string? vehiclePlate = null, string? vehicleRntrc = null,
        string? driverCpf = null, string? driverName = null)
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
            CreatedAt = DateTimeOffset.UtcNow,
            VehiclePlate = vehiclePlate,
            VehicleRntrc = vehicleRntrc,
            DriverCpf = driverCpf,
            DriverName = driverName,
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

    public void UpdateFiscalStatus(string externalId, string fiscalStatus, string? accessKey, string? errorMessage = null)
    {
        FiscalExternalId = externalId;
        FiscalStatus = fiscalStatus;
        FiscalAccessKey = accessKey;
        FiscalErrorMessage = errorMessage;
    }

    public void UpdateMdfeStatus(string externalId, string mdfeStatus, string? accessKey, string? errorMessage = null)
    {
        MdfeExternalId = externalId;
        MdfeStatus = mdfeStatus;
        MdfeAccessKey = accessKey;
        MdfeErrorMessage = errorMessage;
    }

    public void UpdateShippingStatus(string externalId, string status, string? labelUrl, string? trackingCode = null, string? errorMessage = null)
    {
        ShippingExternalId = externalId;
        ShippingStatus = status;
        ShippingLabelUrl = labelUrl;
        if (trackingCode is not null) ShippingTrackingCode = trackingCode;
        ShippingErrorMessage = errorMessage;
    }

    // Clears all fiscal fields so the outbox dispatcher retries emission from scratch.
    public void RequestFiscalRetry()
    {
        if (Status != DispatchStatus.InTransit)
            throw new InvalidOperationException("Only InTransit dispatches can have their fiscal emission retried.");
        FiscalStatus = null;
        FiscalErrorMessage = null;
        FiscalAccessKey = null;
        FiscalExternalId = null;
    }
}
