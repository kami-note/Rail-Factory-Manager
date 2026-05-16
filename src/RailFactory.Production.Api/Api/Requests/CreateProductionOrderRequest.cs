namespace RailFactory.Production.Api.Api.Requests;

public sealed record CreateProductionOrderRequest(Guid BomId, Guid WorkCenterId, decimal PlannedQuantity);
