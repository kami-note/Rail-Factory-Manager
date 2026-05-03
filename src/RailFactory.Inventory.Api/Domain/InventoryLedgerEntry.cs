namespace RailFactory.Inventory.Api.Domain;

public sealed class InventoryLedgerEntry
{
    public Guid Id { get; private set; }
    public Guid BalanceId { get; private set; }
    public string TenantCode { get; private set; }
    public string Operation { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public string DetailsJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private InventoryLedgerEntry()
    {
        TenantCode = string.Empty;
        Operation = string.Empty;
        DetailsJson = "{}";
    }

    private InventoryLedgerEntry(Guid id, Guid balanceId, string tenantCode, string operation, decimal quantityDelta, string detailsJson)
    {
        Id = id;
        BalanceId = balanceId;
        TenantCode = tenantCode;
        Operation = operation;
        QuantityDelta = quantityDelta;
        DetailsJson = detailsJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static InventoryLedgerEntry Create(Guid balanceId, string tenantCode, string operation, decimal quantityDelta, string detailsJson)
    {
        return new InventoryLedgerEntry(Guid.NewGuid(), balanceId, tenantCode, operation, quantityDelta, detailsJson);
    }
}
