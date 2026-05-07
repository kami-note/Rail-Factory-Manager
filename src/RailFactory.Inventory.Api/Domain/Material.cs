using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Represents a unique product or raw material within the inventory catalog.
/// </summary>
/// <remarks>
/// Invariant: <see cref="MaterialCode"/> must be unique per tenant boundary.
/// This entity serves as the centralized source of truth for product metadata, 
/// separating it from physical <see cref="InventoryBalance"/> records.
/// </remarks>
public sealed class Material : AggregateRoot<Guid>
{
    /// <summary>
    /// Unique business identifier (e.g., SKU, Internal Part Number).
    /// </summary>
    public string MaterialCode { get; private set; }

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

    // Private parameterless constructor for EF Core compatibility
    private Material() : base(Guid.Empty)
    {
        MaterialCode = string.Empty;
        OfficialName = string.Empty;
        Description = string.Empty;
        Status = MaterialStatus.Draft;
    }

    private Material(Guid id, string materialCode, string officialName, string description, MaterialCategory category, MaterialStatus status, string? imageUrl, string? ncm, string? gtin)
        : base(id)
    {
        MaterialCode = materialCode;
        OfficialName = officialName;
        Description = description;
        Category = category;
        Status = status;
        ImageUrl = imageUrl;
        Ncm = ncm;
        Gtin = gtin;
    }

    /// <summary>
    /// Factory method to create a new Material instance.
    /// </summary>
    /// <param name="materialCode">Unique material identifier.</param>
    /// <param name="officialName">Standardized name.</param>
    /// <param name="description">Detailed description.</param>
    /// <param name="category">Material category.</param>
    /// <param name="status">Initial validation status.</param>
    /// <param name="imageUrl">Optional image URL.</param>
    /// <param name="ncm">Optional NCM code.</param>
    /// <param name="gtin">Optional GTIN/EAN code.</param>
    /// <returns>A new <see cref="Material"/> instance.</returns>
    public static Material Create(
        string materialCode, 
        string officialName, 
        string description, 
        MaterialCategory category, 
        MaterialStatus status = MaterialStatus.Verified, 
        string? imageUrl = null,
        string? ncm = null,
        string? gtin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(officialName);

        return new Material(Guid.NewGuid(), materialCode.Trim(), officialName.Trim(), description.Trim(), category, status, imageUrl?.Trim(), ncm?.Trim(), gtin?.Trim());
    }

    /// <summary>
    /// Marks the material as verified by the engineering/catalog team.
    /// </summary>
    public void Verify()
    {
        Status = MaterialStatus.Verified;
    }

    /// <summary>
    /// Updates the technical metadata of the material.
    /// </summary>
    public void SetTechnicalMetadata(string? ncm, string? gtin)
    {
        Ncm = ncm?.Trim();
        Gtin = gtin?.Trim();
    }

    /// <summary>
    /// Updates the public image URL for the material catalog entry.
    /// </summary>
    /// <param name="imageUrl">Public image URL.</param>
    public void UpdateImageUrl(string imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);
        ImageUrl = imageUrl.Trim();
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
    Verified = 1
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
