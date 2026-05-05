namespace RailFactory.Inventory.Api.Domain;

public sealed class StockLocation
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    private StockLocation()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private StockLocation(Guid id, string code, string name)
    {
        Id = id;
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
