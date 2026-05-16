namespace RailFactory.Production.Api.Api.Requests;

public sealed record RecordScrapRequest(
    string MaterialCode,
    decimal ScrapQuantity,
    string UnitOfMeasure,
    string Reason);
