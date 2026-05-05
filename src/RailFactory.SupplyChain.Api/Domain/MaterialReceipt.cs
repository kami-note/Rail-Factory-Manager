namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Represents a material receipt within the supply chain boundary.
/// </summary>
/// <remarks>
/// This entity tracks the registration of incoming materials from suppliers,
/// serving as the source of truth for physical inbound operations.
/// It supports both manual entry and automated NF-e (XML) parsing.
/// </remarks>
public sealed class MaterialReceipt
{
    /// <summary>
    /// Unique identifier for the receipt.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Human-readable receipt number (e.g., NFE prefix for fiscal documents).
    /// </summary>
    public string ReceiptNumber { get; private set; }

    /// <summary>
    /// Reference to the supplier aggregate.
    /// </summary>
    public Guid SupplierId { get; private set; }

    /// <summary>
    /// Fiscal document number associated with this receipt.
    /// </summary>
    public string DocumentNumber { get; private set; }

    /// <summary>
    /// The 44-digit NF-e access key, if applicable.
    /// </summary>
    public string? AccessKey { get; private set; }

    /// <summary>
    /// Total fiscal value of the receipt.
    /// </summary>
    public decimal? TotalValue { get; private set; }

    /// <summary>
    /// The original XML content for future reference and audit.
    /// </summary>
    public string? RawXml { get; private set; }

    /// <summary>
    /// The date the material was physically or fiscally received.
    /// </summary>
    public DateOnly ReceiptDate { get; private set; }

    /// <summary>
    /// Current workflow status of the receipt.
    /// </summary>
    public MaterialReceiptStatus Status { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Items included in this receipt.
    /// </summary>
    public List<MaterialReceiptItem> Items { get; private set; }

    private MaterialReceipt()
    {
        ReceiptNumber = string.Empty;
        DocumentNumber = string.Empty;
        Items = [];
    }

    private MaterialReceipt(
        Guid id,
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        string? accessKey,
        decimal? totalValue,
        string? rawXml,
        DateOnly receiptDate)
    {
        Id = id;
        ReceiptNumber = receiptNumber;
        SupplierId = supplierId;
        DocumentNumber = documentNumber;
        AccessKey = accessKey;
        TotalValue = totalValue;
        RawXml = rawXml;
        ReceiptDate = receiptDate;
        Status = MaterialReceiptStatus.Registered;
        CreatedAt = DateTimeOffset.UtcNow;
        Items = [];
    }

    /// <summary>
    /// Factory method for creating a new material receipt.
    /// </summary>
    /// <param name="receiptNumber">Unique receipt identifier.</param>
    /// <param name="supplierId">Supplier reference.</param>
    /// <param name="documentNumber">Fiscal document number.</param>
    /// <param name="accessKey">Optional 44-digit NF-e key.</param>
    /// <param name="totalValue">Optional total fiscal value.</param>
    /// <param name="rawXml">Optional original XML content.</param>
    /// <param name="receiptDate">Date of receipt.</param>
    /// <returns>A new <see cref="MaterialReceipt"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required fields are empty.</exception>
    public static MaterialReceipt Create(
        string receiptNumber,
        Guid supplierId,
        string documentNumber,
        string? accessKey,
        decimal? totalValue,
        string? rawXml,
        DateOnly receiptDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentNumber);

        return new MaterialReceipt(
            Guid.NewGuid(),
            receiptNumber.Trim(),
            supplierId,
            documentNumber.Trim(),
            accessKey?.Trim(),
            totalValue,
            rawXml,
            receiptDate);
    }

    /// <summary>
    /// Adds an item to the receipt.
    /// </summary>
    /// <param name="materialCode">Unique material identifier.</param>
    /// <param name="unitOfMeasure">Unit of measurement (e.g., UN, KG).</param>
    /// <param name="expectedQuantity">The quantity expected based on the document.</param>
    /// <param name="unitPrice">Optional unit price for inventory costing.</param>
    /// <param name="originalDescription">Optional description as found in the original document.</param>
    public void AddItem(string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice = null, string? originalDescription = null)
    {
        Items.Add(MaterialReceiptItem.Create(Id, materialCode, unitOfMeasure, expectedQuantity, unitPrice, originalDescription));
    }
}
