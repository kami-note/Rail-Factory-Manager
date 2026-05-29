namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record AddShipmentItemRequest(
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal WeightKg,
    decimal VolumeCbm);
