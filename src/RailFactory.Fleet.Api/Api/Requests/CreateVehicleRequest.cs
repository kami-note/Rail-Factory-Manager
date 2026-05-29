namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record CreateVehicleRequest(
    string Plate,
    string Chassis,
    string Renavam,
    string Type,
    decimal MaxWeightKg,
    decimal MaxVolumeCbm,
    DateOnly LicenseExpiry);
