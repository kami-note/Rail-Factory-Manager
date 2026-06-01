namespace RailFactory.HumanResources.Api.Api.Requests;

public sealed record CreateWorkShiftRequest(
    DateOnly ShiftDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes);
