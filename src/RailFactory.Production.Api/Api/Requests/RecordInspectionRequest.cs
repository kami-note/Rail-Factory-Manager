namespace RailFactory.Production.Api.Api.Requests;

public sealed record RecordInspectionRequest(string Result, string InspectedBy, string? Notes);
