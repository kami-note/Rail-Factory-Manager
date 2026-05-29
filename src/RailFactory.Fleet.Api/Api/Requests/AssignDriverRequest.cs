namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record AssignDriverRequest(
    Guid DriverPersonId,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);
