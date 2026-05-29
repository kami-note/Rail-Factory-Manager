namespace RailFactory.Fleet.Api.Api.Requests;

public sealed record RecordFuelingRequest(
    DateOnly Date,
    decimal LitersSupplied,
    decimal PricePerLiter,
    int? Odometer,
    string? Supplier,
    string? Notes);
