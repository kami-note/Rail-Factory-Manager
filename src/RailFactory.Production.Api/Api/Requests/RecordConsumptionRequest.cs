namespace RailFactory.Production.Api.Api.Requests;

public sealed record RecordConsumptionRequest(
    string MaterialCode,
    decimal ConsumedQuantity,
    string UnitOfMeasure,
    Guid? InventoryBalanceId);
