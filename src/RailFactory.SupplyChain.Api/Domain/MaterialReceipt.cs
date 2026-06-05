using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Represents a material receipt within the supply chain boundary.
/// </summary>
/// <remarks>
/// This entity tracks the registration of incoming materials from suppliers,
/// serving as the source of truth for physical inbound operations.
/// It supports both manual entry and automated NF-e (XML) parsing.
/// </remarks>
public sealed class MaterialReceipt : AggregateRoot<Guid>
{
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

    /// <summary>SEFAZ tpAmb. Null when unknown (legacy/manual entry).</summary>
    public FiscalEnvironment? FiscalEnvironment { get; private set; }

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
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Items included in this receipt.
    /// </summary>
    public List<MaterialReceiptItem> Items { get; private set; }

    private MaterialReceipt()
        : base(Guid.Empty)
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
        DateOnly receiptDate,
        FiscalEnvironment? fiscalEnvironment = null)
        : base(id)
    {
        ReceiptNumber = receiptNumber;
        SupplierId = supplierId;
        DocumentNumber = documentNumber;
        AccessKey = accessKey;
        TotalValue = totalValue;
        RawXml = rawXml;
        ReceiptDate = receiptDate;
        FiscalEnvironment = fiscalEnvironment;
        Status = MaterialReceiptStatus.Registered;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
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
        DateOnly receiptDate,
        FiscalEnvironment? fiscalEnvironment = null)
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
            receiptDate,
            fiscalEnvironment);
    }

    /// <summary>
    /// Adds an item to the receipt.
    /// </summary>
    public void AddItem(string materialCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice = null, string? originalDescription = null, string? ncm = null, string? cfop = null, string? ean = null)
    {
        Items.Add(MaterialReceiptItem.Create(Id, materialCode, unitOfMeasure, expectedQuantity, unitPrice, originalDescription, ncm, cfop, ean));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds an item that was resolved through an existing supplier mapping.
    /// </summary>
    public void AddMappedItem(
        string supplierProductCode,
        string internalMaterialCode,
        decimal supplierQuantity,
        string supplierUnitOfMeasure,
        string unitOfMeasure,
        decimal expectedQuantity,
        decimal? unitPrice = null,
        string? originalDescription = null,
        string? ncm = null,
        string? cfop = null,
        string? ean = null,
        decimal? conversionFactor = null)
    {
        Items.Add(MaterialReceiptItem.CreateMapped(
            Id,
            supplierProductCode,
            internalMaterialCode,
            supplierQuantity,
            supplierUnitOfMeasure,
            unitOfMeasure,
            expectedQuantity,
            unitPrice,
            originalDescription,
            ncm,
            cfop,
            ean,
            conversionFactor));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds an item that still requires supplier SKU to internal SKU association.
    /// </summary>
    public void AddPendingAssociationItem(string supplierProductCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice = null, string? originalDescription = null, string? ncm = null, string? cfop = null, string? ean = null)
    {
        Items.Add(MaterialReceiptItem.CreatePendingAssociation(Id, supplierProductCode, unitOfMeasure, expectedQuantity, unitPrice, originalDescription, ncm, cfop, ean));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Blocks the receipt from proceeding because one or more supplier material codes are unmapped.
    /// </summary>
    public void BlockForAssociation()
    {
        if (Status != MaterialReceiptStatus.Registered)
        {
            throw new InvalidOperationException($"Cannot block receipt for association in status '{Status}'. Only 'Registered' receipts can be blocked.");
        }

        Status = MaterialReceiptStatus.PendingAssociation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Releases a receipt blocked by supplier SKU association back to the normal conference-ready state.
    /// </summary>
    public void ReleaseAssociation()
    {
        if (Status != MaterialReceiptStatus.PendingAssociation)
        {
            throw new InvalidOperationException($"Cannot release association for receipt in status '{Status}'. Only 'PendingAssociation' receipts can be released.");
        }

        var unresolvedItems = Items.Where(x => x.AssociationStatus is not MaterialReceiptItemAssociationStatus.Mapped and not MaterialReceiptItemAssociationStatus.CreatedAndMapped).ToList();
        if (unresolvedItems.Count > 0)
        {
            throw new InvalidOperationException("Cannot release receipt while association items are unresolved.");
        }

        Status = MaterialReceiptStatus.Registered;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Starts the blind conference process for this receipt.
    /// </summary>
    /// <remarks>
    /// Invariant: Only receipts in 'Registered' status can be started.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when status is not Registered.</exception>
    public void StartConference()
    {
        if (Status != MaterialReceiptStatus.Registered)
        {
            throw new InvalidOperationException($"Cannot start conference for receipt in status '{Status}'. Only 'Registered' receipts can be started.");
        }

        Status = MaterialReceiptStatus.InConference;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Closes the conference process and evaluates divergences.
    /// </summary>
    /// <param name="results">The counted results for each item.</param>
    /// <exception cref="InvalidOperationException">Thrown if status is not InConference.</exception>
    public void CloseConference(IReadOnlyCollection<CountedItemResult> results)
    {
        if (Status != MaterialReceiptStatus.InConference)
        {
            throw new InvalidOperationException($"Cannot close conference for receipt in status '{Status}'. Only 'InConference' receipts can be closed.");
        }

        foreach (var result in results)
        {
            var item = Items.FirstOrDefault(x => x.Id == result.ItemId)
                ?? throw new InvalidOperationException($"Item with ID '{result.ItemId}' not found in this receipt.");

            item.RecordConference(result.CountedQuantity, result.ConfirmedLotNumber, result.ConfirmedExpirationDate);
        }

        Status = Items.Any(x => x.HasDivergence)
            ? MaterialReceiptStatus.Divergent
            : MaterialReceiptStatus.Approved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Domain result record for conference counting.
/// </summary>
public record CountedItemResult(Guid ItemId, decimal CountedQuantity, string? ConfirmedLotNumber, DateTimeOffset? ConfirmedExpirationDate);
