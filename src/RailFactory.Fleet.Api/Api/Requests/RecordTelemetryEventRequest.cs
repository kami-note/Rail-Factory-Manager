namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record RecordTelemetryEventRequest(
    Guid? DriverPersonId,
    string EventType,
    string Description,
    DateTimeOffset OccurredAt,
    decimal? LatitudeDeg,
    decimal? LongitudeDeg);
