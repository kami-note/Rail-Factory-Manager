namespace RailFactory.Production.Api.Api.Requests;

public sealed record CreateBomRequest(string ProductCode, decimal? BatchSize);
