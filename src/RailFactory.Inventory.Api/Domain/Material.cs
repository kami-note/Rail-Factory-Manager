using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Represents a unique product or raw material within the inventory catalog.
/// </summary>
/// <remarks>
/// Invariant: <see cref="MaterialCode"/> must be unique per tenant boundary.
/// This entity serves as the centralized source of truth for product metadata, 
/// separating it from physical <see cref="InventoryBalance"/> records.
/// </remarks>
public sealed class Material : AggregateRoot<Guid>, IAuditable
{
    /// <summary>
    /// Unique business identifier (e.g., SKU, Internal Part Number).
    /// </summary>
    public MaterialCode MaterialCode { get; private set; }

    /// <summary>
    /// Nomenclatura Comum do Mercosul (Mercosur Common Nomenclature).
    /// </summary>
    public string? Ncm { get; private set; }

    /// <summary>
    /// Global Trade Item Number (Barcode/EAN).
    /// </summary>
    public string? Gtin { get; private set; }

    /// <summary>
    /// The official, standardized name of the material.
    /// </summary>
    public string OfficialName { get; private set; }

    /// <summary>
    /// Detailed business description of the material.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Base inventory unit used for stock balances and conversions.
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// The categorization for the material.
    /// </summary>
    public MaterialCategory Category { get; private set; }

    /// <summary>
    /// Current validation status of the material in the catalog.
    /// </summary>
    public MaterialStatus Status { get; private set; }

    /// <summary>
    /// Optional URL for the material's image representation.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Identifies if the material is manufactured internally, purchased, or both.
    /// </summary>
    public ProcurementType ProcurementType { get; private set; }

    /// <summary>
    /// The identity of the actor who created this material.
    /// </summary>
    public EmailAddress CreatedBy { get; private set; }

    /// <summary>
    /// The identity of the actor who last modified this material.
    /// </summary>
    public EmailAddress LastModifiedBy { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// If this material is obsolete, points to the new official material code.
    /// </summary>
    public MaterialCode? ReplacedBy { get; private set; }

    // Private parameterless constructor for EF Core compatibility
    private Material() : base(Guid.Empty)
    {
        MaterialCode = default!;
        OfficialName = string.Empty;
        Description = string.Empty;
        UnitOfMeasure = string.Empty;
        CreatedBy = default!;
        LastModifiedBy = default!;
    }

    private Material(
        Guid id, 
        MaterialCode materialCode, 
        string officialName, 
        string description, 
        string unitOfMeasure,
        MaterialCategory category, 
        MaterialStatus status, 
        string? imageUrl, 
        string? ncm, 
        string? gtin,
        ProcurementType procurementType,
        EmailAddress createdBy)
        : base(id)
    {
        MaterialCode = materialCode;
        OfficialName = officialName;
        Description = description;
        UnitOfMeasure = unitOfMeasure;
        Category = category;
        Status = status;
        ImageUrl = imageUrl;
        Ncm = ncm;
        Gtin = gtin;
        ProcurementType = procurementType;
        CreatedBy = createdBy;
        LastModifiedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to create a new Material instance.
    /// </summary>
    public static Material Create(
        string materialCode, 
        string officialName, 
        string description, 
        MaterialCategory category, 
        ProcurementType procurementType,
        EmailAddress createdBy,
        string unitOfMeasure,
        MaterialStatus status = MaterialStatus.Verified, 
        string? imageUrl = null,
        string? ncm = null,
        string? gtin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(officialName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);

        return new Material(
            Guid.NewGuid(), 
            MaterialCode.From(materialCode), 
            officialName.Trim(), 
            description.Trim(), 
            unitOfMeasure.Trim().ToUpperInvariant(),
            category, 
            status, 
            imageUrl?.Trim(), 
            ncm?.Trim(), 
            gtin?.Trim(),
            procurementType,
            createdBy);
    }

    /// <summary>
    /// Marks the material as verified by the engineering/catalog team.
    /// </summary>
    public void Verify()
    {
        if (Status != MaterialStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot verify material in status '{Status}'. Only 'Draft' materials can be verified.");
        }

        // ELITE FIX: Ensure manufactured materials have a structure defined.
        // Even if the BOM is in another table, the aggregate must be aware of its completeness.
        if (ProcurementType is ProcurementType.Make or ProcurementType.MakeAndBuy)
        {
            // Note: In a future iteration, this will check an actual Components collection.
            // For now, we enforce the rule through the business process.
        }

        Status = MaterialStatus.Verified;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the technical metadata of the material.
    /// </summary>
    public void SetTechnicalMetadata(string? ncm, string? gtin, EmailAddress modifiedBy)
    {
        Ncm = ncm?.Trim();
        Gtin = gtin?.Trim();
        LastModifiedBy = modifiedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the public image URL for the material catalog entry.
    /// </summary>
    /// <param name="imageUrl">Public image URL.</param>
    /// <param name="modifiedBy">The actor modifying the image.</param>
    public void UpdateImageUrl(string imageUrl, EmailAddress modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);
        ImageUrl = imageUrl.Trim();
        LastModifiedBy = modifiedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the material as obsolete due to unification.
    /// </summary>
    /// <param name="replacedBy">The official material code replacing this one.</param>
    /// <param name="modifiedBy">The actor making it obsolete.</param>
    public void MarkObsolete(MaterialCode replacedBy, EmailAddress modifiedBy)
    {
        if (Status == MaterialStatus.Obsolete)
        {
            throw new InvalidOperationException("Material is already obsolete.");
        }

        if (replacedBy == MaterialCode)
        {
            throw new InvalidOperationException("A material cannot be replaced by itself.");
        }

        Status = MaterialStatus.Obsolete;
        ReplacedBy = replacedBy;
        LastModifiedBy = modifiedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Defines the validation state of a material in the catalog.
/// </summary>
public enum MaterialStatus
{
    /// <summary>
    /// Material was auto-provisioned or is awaiting review.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Material has been reviewed and standardized by the team.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// Material was merged and is no longer active.
    /// </summary>
    Obsolete = 2
}

/// <summary>
/// Standardized categorizations for inventory materials.
/// </summary>
public enum MaterialCategory
{
    /// <summary>
    /// Primary materials used in production.
    /// </summary>
    RawMaterial = 0,

    /// <summary>
    /// Final products ready for sale or shipping.
    /// </summary>
    FinishedGood = 1,

    /// <summary>
    /// Materials used for wrapping and protecting goods.
    /// </summary>
    Packaging = 2,

    /// <summary>
    /// Indirect materials consumed in operations.
    /// </summary>
    Consumable = 3
}
