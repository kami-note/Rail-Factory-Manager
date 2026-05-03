namespace RailFactory.SupplyChain.Api.Domain;

public sealed class MaterialReceipt
{
    public Guid Id { get; private set; }
    public string ReceiptNumber { get; private set; }
    public Guid SupplierId { get; private set; }
    public string DocumentNumber { get; private set; }
    public DateOnly ReceiptDate { get; private set; }
    public string TenantCode { get; private set; }
    public MaterialReceiptStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<MaterialReceiptItem> Items { get; private set; }

    private MaterialReceipt()
    {
        ReceiptNumber = string.Empty;
        DocumentNumber = string.Empty;
        TenantCode = string.Empty;
        Items = [];
    }

    private MaterialReceipt(
        Guid id,
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        DateOnly receiptDate,
        string tenantCode)
    {
        Id = id;
        ReceiptNumber = receiptNumber;
        SupplierId = supplierId;
        DocumentNumber = documentNumber;
        ReceiptDate = receiptDate;
        TenantCode = tenantCode;
        Status = MaterialReceiptStatus.Registered;
        CreatedAt = DateTimeOffset.UtcNow;
        Items = [];
    }

    public static MaterialReceipt Create(
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        DateOnly receiptDate,
        string tenantCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);

        return new MaterialReceipt(
            Guid.NewGuid(),
            receiptNumber.Trim(),
            supplierId,
            documentNumber.Trim(),
            receiptDate,
            tenantCode.Trim());
    }

    public void AddItem(string materialCode, decimal expectedQuantity, string unitOfMeasure)
    {
        Items.Add(MaterialReceiptItem.Create(Id, materialCode, expectedQuantity, unitOfMeasure));
    }
}
