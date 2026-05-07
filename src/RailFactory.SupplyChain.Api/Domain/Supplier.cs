using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.SupplyChain.Api.Domain;

public sealed class Supplier : AggregateRoot<Guid>
{
    public string FiscalId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Supplier()
        : base(Guid.Empty)
    {
        FiscalId = string.Empty;
        Name = string.Empty;
    }

    private Supplier(Guid id, string fiscalId, string name)
        : base(id)
    {
        FiscalId = fiscalId;
        Name = name;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Supplier Create(string fiscalId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fiscalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Supplier(Guid.NewGuid(), fiscalId.Trim(), name.Trim());
    }
}
