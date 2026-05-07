using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Inventory.Api.Domain;

public sealed class StockLocation : AggregateRoot<Guid>
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    private StockLocation()
        : base(Guid.Empty)
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private StockLocation(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    public static StockLocation Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new StockLocation(Guid.NewGuid(), code.Trim(), name.Trim());
    }
}
