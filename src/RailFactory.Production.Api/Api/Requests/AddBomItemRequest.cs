namespace RailFactory.Production.Api.Api.Requests;

public sealed record AddBomItemRequest(string MaterialCode, decimal Quantity, string UnitOfMeasure);
