using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.SupplyChain.Api.Domain;

public sealed class Supplier : AggregateRoot<Guid>
{
    public FiscalId FiscalId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Supplier()
        : base(Guid.Empty)
    {
        FiscalId = default!;
        Name = string.Empty;
    }

    private Supplier(Guid id, FiscalId fiscalId, string name)
        : base(id)
    {
        FiscalId = fiscalId;
        Name = name;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Supplier Create(string fiscalId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Supplier(Guid.NewGuid(), FiscalId.From(fiscalId), name.Trim());
    }
}
