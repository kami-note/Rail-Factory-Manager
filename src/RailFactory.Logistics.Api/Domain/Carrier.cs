namespace RailFactory.Logistics.Api.Domain;

public enum CarrierStatus { Active, Inactive }

public sealed class Carrier
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public decimal RatePerKg { get; private set; }
    public decimal RatePerCbm { get; private set; }
    public CarrierStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Carrier() { }

    public static Carrier Create(string name, string documentNumber, string? contactEmail,
        decimal ratePerKg, decimal ratePerCbm)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.", nameof(documentNumber));

        var now = DateTimeOffset.UtcNow;
        return new Carrier
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            DocumentNumber = documentNumber.Trim(),
            ContactEmail = contactEmail?.Trim(),
            RatePerKg = ratePerKg,
            RatePerCbm = ratePerCbm,
            Status = CarrierStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Activate()
    {
        Status = CarrierStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = CarrierStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
