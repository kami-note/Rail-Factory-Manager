namespace RailFactory.HumanResources.Api.Api.Requests;

public sealed record CreatePersonRequest(
    string Name,
    string DocumentNumber,
    string Type,
    string? Email);
