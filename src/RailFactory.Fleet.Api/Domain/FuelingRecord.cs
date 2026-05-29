namespace RailFactory.Fleet.Api.Domain;

public sealed class FuelingRecord
{
    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal LitersSupplied { get; private set; }
    public decimal PricePerLiter { get; private set; }
    public int? Odometer { get; private set; }
    public string? Supplier { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private FuelingRecord() { }

    public static FuelingRecord Create(
        Guid vehicleId, DateOnly date, decimal litersSupplied,
        decimal pricePerLiter, int? odometer, string? supplier, string? notes)
    {
        if (litersSupplied <= 0)
            throw new ArgumentException("Liters supplied must be positive.", nameof(litersSupplied));
        if (pricePerLiter <= 0)
            throw new ArgumentException("Price per liter must be positive.", nameof(pricePerLiter));

        return new FuelingRecord
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            Date = date,
            LitersSupplied = litersSupplied,
            PricePerLiter = pricePerLiter,
            Odometer = odometer,
            Supplier = supplier?.Trim(),
            Notes = notes?.Trim(),
            RecordedAt = DateTimeOffset.UtcNow
        };
    }
}
