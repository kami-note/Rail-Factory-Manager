using RailFactory.BuildingBlocks.Tenancy;

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
    public MaterialCode MaterialCode { get; private set; }

    /// <summary>
    /// Supplier SKU from the fiscal document (`cProd`). This preserves the source identity even after mapping.
    /// </summary>
    public string SupplierProductCode { get; private set; }

    /// <summary>
    /// Original quantity from the supplier fiscal document before any stock-unit conversion.
    /// </summary>
    public decimal SupplierQuantity { get; private set; }

    /// <summary>
    /// Original unit from the supplier fiscal document before any stock-unit conversion.
    /// </summary>
    public string SupplierUnitOfMeasure { get; private set; }

    /// <summary>
    /// Internal Inventory SKU selected by an operator or existing supplier mapping.
    /// </summary>
    public MaterialCode? InternalMaterialCode { get; private set; }

    /// <summary>
    /// Current item-level association decision state.
    /// </summary>
    public MaterialReceiptItemAssociationStatus AssociationStatus { get; private set; }

    /// <summary>
    /// Quantity multiplier from supplier unit to stock unit.
    /// </summary>
    public decimal? AssociationConversionFactor { get; private set; }

    /// <summary>
    /// Required human reason for review/ignore/override decisions.
    /// </summary>
    public string? AssociationReason { get; private set; }

    /// <summary>
    /// Version token used by the Workbench to detect concurrent item updates.
    /// </summary>
    public DateTimeOffset AssociationUpdatedAt { get; private set; }

    /// <summary>
    /// Last actor who changed the association decision.
    /// </summary>
    public string? AssociationUpdatedBy { get; private set; }

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
        MaterialCode = default!;
        SupplierProductCode = string.Empty;
        SupplierUnitOfMeasure = string.Empty;
        UnitOfMeasure = string.Empty;
    }

    private MaterialReceiptItem(
        Guid id,
        Guid receiptId,
        MaterialCode materialCode,
        string supplierProductCode,
        MaterialCode? internalMaterialCode,
        MaterialReceiptItemAssociationStatus associationStatus,
        decimal? associationConversionFactor,
        decimal supplierQuantity,
        string supplierUnitOfMeasure,
        string unitOfMeasure,
        decimal expectedQuantity,
        decimal? unitPrice,
        string? originalDescription,
        string? ncm,
        string? cfop,
        string? ean)
    {
        Id = id;
        ReceiptId = receiptId;
        MaterialCode = materialCode;
        SupplierProductCode = supplierProductCode;
        SupplierQuantity = supplierQuantity;
        SupplierUnitOfMeasure = supplierUnitOfMeasure;
        InternalMaterialCode = internalMaterialCode;
        AssociationStatus = associationStatus;
        AssociationConversionFactor = associationConversionFactor;
        AssociationUpdatedAt = DateTimeOffset.UtcNow;
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
        => CreateMapped(receiptId, materialCode, materialCode, expectedQuantity, unitOfMeasure, unitOfMeasure, expectedQuantity, unitPrice, originalDescription, ncm, cfop, ean, conversionFactor: 1);

    /// <summary>
    /// Creates an item that is already mapped to an internal Inventory material.
    /// </summary>
    public static MaterialReceiptItem CreateMapped(
        Guid receiptId,
        string supplierProductCode,
        string internalMaterialCode,
        decimal supplierQuantity,
        string supplierUnitOfMeasure,
        string unitOfMeasure,
        decimal expectedQuantity,
        decimal? unitPrice,
        string? originalDescription,
        string? ncm = null,
        string? cfop = null,
        string? ean = null,
        decimal? conversionFactor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(internalMaterialCode);
        var item = CreateCore(
            receiptId,
            internalMaterialCode,
            supplierProductCode,
            MaterialCode.From(internalMaterialCode),
            MaterialReceiptItemAssociationStatus.Mapped,
            conversionFactor,
            supplierQuantity,
            supplierUnitOfMeasure,
            unitOfMeasure,
            expectedQuantity,
            unitPrice,
            originalDescription,
            ncm,
            cfop,
            ean);

        return item;
    }

    /// <summary>
    /// Creates an item that still needs supplier SKU to internal SKU association.
    /// </summary>
    public static MaterialReceiptItem CreatePendingAssociation(Guid receiptId, string supplierProductCode, string unitOfMeasure, decimal expectedQuantity, decimal? unitPrice, string? originalDescription, string? ncm = null, string? cfop = null, string? ean = null)
        => CreateCore(
            receiptId,
            supplierProductCode,
            supplierProductCode,
            internalMaterialCode: null,
            MaterialReceiptItemAssociationStatus.Pending,
            associationConversionFactor: null,
            expectedQuantity,
            unitOfMeasure,
            unitOfMeasure,
            expectedQuantity,
            unitPrice,
            originalDescription,
            ncm,
            cfop,
            ean);

    private static MaterialReceiptItem CreateCore(
        Guid receiptId,
        string materialCode,
        string supplierProductCode,
        MaterialCode? internalMaterialCode,
        MaterialReceiptItemAssociationStatus associationStatus,
        decimal? associationConversionFactor,
        decimal supplierQuantity,
        string supplierUnitOfMeasure,
        string unitOfMeasure,
        decimal expectedQuantity,
        decimal? unitPrice,
        string? originalDescription,
        string? ncm = null,
        string? cfop = null,
        string? ean = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierProductCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierUnitOfMeasure);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        if (supplierQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(supplierQuantity), "Supplier quantity must be greater than zero.");
        }

        if (expectedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedQuantity), "Expected quantity must be greater than zero.");
        }

        if (associationConversionFactor is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(associationConversionFactor), "Association conversion factor must be greater than zero.");
        }

        return new MaterialReceiptItem(
            Guid.NewGuid(),
            receiptId,
            MaterialCode.From(materialCode),
            supplierProductCode.Trim(),
            internalMaterialCode,
            associationStatus,
            associationConversionFactor,
            supplierQuantity,
            supplierUnitOfMeasure.Trim(),
            unitOfMeasure.Trim(),
            expectedQuantity,
            unitPrice,
            originalDescription?.Trim(),
            ncm?.Trim(),
            cfop?.Trim(),
            ean?.Trim());
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
    /// Resolves this supplier item to an internal Inventory material.
    /// </summary>
    public void MapToExistingMaterial(string internalMaterialCode, string internalUnitOfMeasure, decimal conversionFactor, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(internalMaterialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(internalUnitOfMeasure);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (conversionFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conversionFactor), "Conversion factor must be greater than zero.");
        }

        var convertedQuantity = SupplierQuantity * conversionFactor;
        MaterialCode = MaterialCode.From(internalMaterialCode);
        InternalMaterialCode = MaterialCode;
        UnitOfMeasure = internalUnitOfMeasure.Trim().ToUpperInvariant();
        ExpectedQuantity = convertedQuantity;
        UnitPrice = UnitPrice.HasValue
            ? Math.Round(UnitPrice.Value / conversionFactor, PrecisionConstants.DefaultDecimalPlaces, MidpointRounding.AwayFromZero)
            : null;
        AssociationStatus = MaterialReceiptItemAssociationStatus.Mapped;
        AssociationConversionFactor = conversionFactor;
        AssociationReason = null;
        AssociationUpdatedAt = DateTimeOffset.UtcNow;
        AssociationUpdatedBy = actor.Trim();
    }

    /// <summary>
    /// Resolves this supplier item by creating a new internal Inventory material.
    /// </summary>
    public void MapToNewMaterial(string internalMaterialCode, string internalUnitOfMeasure, decimal conversionFactor, string actor)
    {
        MapToExistingMaterial(internalMaterialCode, internalUnitOfMeasure, conversionFactor, actor);
        AssociationStatus = MaterialReceiptItemAssociationStatus.CreatedAndMapped;
    }

    /// <summary>
    /// Marks the item as requiring later review before the receipt can proceed.
    /// </summary>
    public void MarkReviewLater(string reason, string actor)
    {
        MarkControlledDecision(MaterialReceiptItemAssociationStatus.ReviewLater, reason, actor);
    }

    /// <summary>
    /// Marks the item as intentionally ignored. This remains blocking until business rules allow release.
    /// </summary>
    public void MarkIgnored(string reason, string actor)
    {
        MarkControlledDecision(MaterialReceiptItemAssociationStatus.Ignored, reason, actor);
    }

    private void MarkControlledDecision(MaterialReceiptItemAssociationStatus status, string reason, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        AssociationStatus = status;
        AssociationReason = reason.Trim();
        AssociationUpdatedAt = DateTimeOffset.UtcNow;
        AssociationUpdatedBy = actor.Trim();
    }

    /// <summary>
    /// Overrides the supplier product code from the invoice with a corrected value.
    /// This is an exceptional auditable operation.
    /// </summary>
    public void OverrideSupplierProductCode(string correctedCode, string reason, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correctedCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        SupplierProductCode = correctedCode.Trim();
        AssociationReason = reason.Trim();
        AssociationUpdatedAt = DateTimeOffset.UtcNow;
        AssociationUpdatedBy = actor.Trim();
    }

    /// <summary>
    /// Checks if there is a quantity divergence between expected and counted.
    /// </summary>
    public bool HasDivergence => CountedQuantity.HasValue && CountedQuantity != ExpectedQuantity;
}
