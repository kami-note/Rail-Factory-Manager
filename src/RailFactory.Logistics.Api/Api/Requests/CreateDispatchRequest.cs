namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record CreateDispatchRequest(
    Guid ShipmentOrderId,
    Guid CarrierId,
    Guid VehicleId,
    Guid DriverPersonId,
    string? VehiclePlate = null,
    string? VehicleRntrc = null,
    string? DriverCpf = null,
    string? DriverName = null);
