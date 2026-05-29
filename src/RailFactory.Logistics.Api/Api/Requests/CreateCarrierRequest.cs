namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record CreateCarrierRequest(
    string Name,
    string DocumentNumber,
    string? ContactEmail,
    decimal RatePerKg,
    decimal RatePerCbm);
