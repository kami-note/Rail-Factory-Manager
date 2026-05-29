namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record CreateShipmentOrderRequest(Guid? ProductionOrderRef, string? Notes);
