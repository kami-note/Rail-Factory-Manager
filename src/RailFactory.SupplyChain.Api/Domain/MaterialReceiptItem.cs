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
    /// Nomenclatura Comum do Mercosul (Mercosur Common Nomenclature).
    /// </summary>
    public string? Ncm { get; private set; }

    /// <summary>
    /// Código Fiscal de Operações e Prestações (Tax Operation Code).
    /// </summary>
    public string? Cfop { get; private set; }

    /// <summary>
    /// Global Trade Item Number (Barcode/EAN).
    /// </summary>
    public string? Ean { get; private set; }

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

    /// <summary>
    /// The actual quantity counted during physical conference.
    /// </summary>
    public decimal? CountedQuantity { get; private set; }

    /// <summary>
    /// The lot number confirmed by the operator during conference.
    /// </summary>
    public string? ConfirmedLotNumber { get; private set; }

    /// <summary>
    /// The expiration date confirmed by the operator during conference.
    /// </summary>
    public DateTimeOffset? ConfirmedExpirationDate { get; private set; }

    private MaterialReceiptItem()
    {
        MaterialCode = string.Empty;
        UnitOfMeasure = string.Empty;
    }

    private MaterialReceiptItem(Guid id, Guid receiptId, string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice, string? originalDescription, string? ncm, string? cfop, string? ean)
    {
        Id = id;
        ReceiptId = receiptId;
        MaterialCode = materialCode;
        UnitOfMeasure = unitOfMeasure;
        ExpectedQuantity = expectedQuantity;
        UnitPrice = unitPrice;
        OriginalDescription = originalDescription;
        Ncm = ncm;
        Cfop = cfop;
        Ean = ean;
    }

    /// <summary>
    /// Factory method for creating a new receipt item.
    /// </summary>
    public static MaterialReceiptItem Create(Guid receiptId, string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice, string? originalDescription, string? ncm = null, string? cfop = null, string? ean = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        if (expectedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedQuantity), "Expected quantity must be greater than zero.");
        }

        return new MaterialReceiptItem(Guid.NewGuid(), receiptId, materialCode.Trim(), unitOfMeasure.Trim(), expectedQuantity, unitPrice, originalDescription?.Trim(), ncm?.Trim(), cfop?.Trim(), ean?.Trim());
    }

    /// <summary>
    /// Records the results of a physical conference for this item.
    /// </summary>
    public void RecordConference(decimal quantity, string? lotNumber, DateTimeOffset? expirationDate)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Counted quantity cannot be negative.");
        }

        CountedQuantity = quantity;
        ConfirmedLotNumber = lotNumber?.Trim();
        ConfirmedExpirationDate = expirationDate;
    }

    /// <summary>
    /// Checks if there is a quantity divergence between expected and counted.
    /// </summary>
    public bool HasDivergence => CountedQuantity.HasValue && CountedQuantity != ExpectedQuantity;
}
