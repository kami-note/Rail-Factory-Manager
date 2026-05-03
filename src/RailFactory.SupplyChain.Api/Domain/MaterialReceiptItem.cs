namespace RailFactory.SupplyChain.Api.Domain;

public sealed class MaterialReceiptItem
{
    public Guid Id { get; private set; }
    public Guid ReceiptId { get; private set; }
    public string MaterialCode { get; private set; }
    public decimal ExpectedQuantity { get; private set; }
    public string UnitOfMeasure { get; private set; }

    private MaterialReceiptItem()
    {
        MaterialCode = string.Empty;
        UnitOfMeasure = string.Empty;
    }

    private MaterialReceiptItem(Guid id, Guid receiptId, string materialCode, decimal expectedQuantity, string unitOfMeasure)
    {
        Id = id;
        ReceiptId = receiptId;
        MaterialCode = materialCode;
        ExpectedQuantity = expectedQuantity;
        UnitOfMeasure = unitOfMeasure;
    }

    public static MaterialReceiptItem Create(Guid receiptId, string materialCode, decimal expectedQuantity, string unitOfMeasure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        if (expectedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedQuantity), "Expected quantity must be greater than zero.");
        }

        return new MaterialReceiptItem(Guid.NewGuid(), receiptId, materialCode.Trim(), expectedQuantity, unitOfMeasure.Trim());
    }
}
