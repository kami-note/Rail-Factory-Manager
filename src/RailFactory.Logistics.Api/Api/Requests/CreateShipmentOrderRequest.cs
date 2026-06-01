namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record CreateShipmentOrderRequest(
    Guid? ProductionOrderRef,
    string? Notes,
    decimal? DeliveryLatitudeDeg = null,
    decimal? DeliveryLongitudeDeg = null,
    string? DeliveryCity = null);
