namespace RailFactory.Inventory.Api.Domain;

public sealed class InventoryBalance
{
    public Guid Id { get; private set; }
    public string TenantCode { get; private set; }
    public string MaterialCode { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public decimal Quantity { get; private set; }
    public InventoryBalanceStatus Status { get; private set; }
    public Guid StockLocationId { get; private set; }
    public string SourceReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private InventoryBalance()
    {
        TenantCode = string.Empty;
        MaterialCode = string.Empty;
        UnitOfMeasure = string.Empty;
        SourceReference = string.Empty;
    }

    private InventoryBalance(
        Guid id,
        string tenantCode,
        string materialCode,
        string unitOfMeasure,
        decimal quantity,
        Guid stockLocationId,
        string sourceReference)
    {
        Id = id;
        TenantCode = tenantCode;
        MaterialCode = materialCode;
        UnitOfMeasure = unitOfMeasure;
        Quantity = quantity;
        Status = InventoryBalanceStatus.Pending;
        StockLocationId = stockLocationId;
        SourceReference = sourceReference;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static InventoryBalance CreatePending(
        string tenantCode,
        string materialCode,
        string unitOfMeasure,
        decimal quantity,
        Guid stockLocationId,
        string sourceReference)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        return new InventoryBalance(
            Guid.NewGuid(),
            tenantCode.Trim(),
            materialCode.Trim(),
            unitOfMeasure.Trim(),
            quantity,
            stockLocationId,
            sourceReference.Trim());
    }
}
