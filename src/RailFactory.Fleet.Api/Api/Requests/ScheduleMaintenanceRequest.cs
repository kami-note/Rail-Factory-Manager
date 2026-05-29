namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record ScheduleMaintenanceRequest(
    string Type,
    string Description,
    DateOnly ScheduledDate,
    string? Notes);
