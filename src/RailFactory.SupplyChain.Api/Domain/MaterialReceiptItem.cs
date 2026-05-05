namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Represents a specific item within a material receipt.
/// </summary>
public sealed class MaterialReceiptItem
{
    /// <summary>
    /// Unique identifier for the receipt item.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Reference to the parent <see cref="MaterialReceipt"/>.
    /// </summary>
    public Guid ReceiptId { get; private set; }

    /// <summary>
    /// Unique code for the material (SKU).
    /// </summary>
    public string MaterialCode { get; private set; }

    /// <summary>
    /// The quantity of material expected as per the document.
    /// </summary>
    public decimal ExpectedQuantity { get; private set; }

    /// <summary>
    /// Unit of measurement (e.g., UN, KG).
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// Optional unit price as found in the source document.
    /// </summary>
    public decimal? UnitPrice { get; private set; }

    /// <summary>
    /// Original description of the item from the source document (e.g., NF-e xProd).
    /// </summary>
    public string? OriginalDescription { get; private set; }

    private MaterialReceiptItem()
    {
        MaterialCode = string.Empty;
        UnitOfMeasure = string.Empty;
    }

    private MaterialReceiptItem(Guid id, Guid receiptId, string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice, string? originalDescription)
    {
        Id = id;
        ReceiptId = receiptId;
        MaterialCode = materialCode;
        UnitOfMeasure = unitOfMeasure;
        ExpectedQuantity = expectedQuantity;
        UnitPrice = unitPrice;
        OriginalDescription = originalDescription;
    }

    /// <summary>
    /// Factory method for creating a new receipt item.
    /// </summary>
    /// <param name="receiptId">Parent receipt reference.</param>
    /// <param name="materialCode">Material SKU.</param>
    /// <param name="unitOfMeasure">Unit of measure.</param>
    /// <param name="expectedQuantity">Expected quantity.</param>
    /// <param name="unitPrice">Optional unit price.</param>
    /// <param name="originalDescription">Optional original description.</param>
    /// <returns>A new <see cref="MaterialReceiptItem"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when quantity is not positive.</exception>
    public static MaterialReceiptItem Create(Guid receiptId, string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice, string? originalDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        if (expectedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedQuantity), "Expected quantity must be greater than zero.");
        }

        return new MaterialReceiptItem(Guid.NewGuid(), receiptId, materialCode.Trim(), unitOfMeasure.Trim(), expectedQuantity, unitPrice, originalDescription?.Trim());
    }
}
