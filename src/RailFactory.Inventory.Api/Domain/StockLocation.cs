namespace RailFactory.Inventory.Api.Domain;

public sealed class StockLocation
{
    public Guid Id { get; private set; }
    public string TenantCode { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    private StockLocation()
    {
        TenantCode = string.Empty;
        Code = string.Empty;
        Name = string.Empty;
    }

    private StockLocation(Guid id, string tenantCode, string code, string name)
    {
        Id = id;
        TenantCode = tenantCode;
        Code = code;
        Name = name;
        IsActive = true;
    }

    public static StockLocation Create(string tenantCode, string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new StockLocation(Guid.NewGuid(), tenantCode.Trim(), code.Trim(), name.Trim());
    }
}
