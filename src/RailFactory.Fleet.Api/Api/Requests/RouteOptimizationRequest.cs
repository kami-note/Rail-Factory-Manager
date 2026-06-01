namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record RouteOptimizationRequest(List<RouteStopRequest> Stops);

public sealed record RouteStopRequest(string Id, string Label, decimal Lat, decimal Lon);
