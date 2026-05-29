namespace RailFactory.HumanResources.Api.Api.Requests;

public sealed record LogHoursRequest(
    DateOnly Date,
    decimal HoursWorked,
    string? Description);
