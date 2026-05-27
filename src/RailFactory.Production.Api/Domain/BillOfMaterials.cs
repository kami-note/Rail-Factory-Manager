using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Represents a versioned recipe of input materials required to produce a finished product.
/// </summary>
/// <remarks>
/// Invariant: Only one version of a BOM per <see cref="ProductCode"/> can have <see cref="BomStatus.Active"/> status.
/// When a new version is activated, the previously active version is automatically reverted to
/// <see cref="BomStatus.Draft"/>. This transition is performed atomically within <see cref="Activate"/>.
/// Invariant: A BOM cannot be activated without at least one <see cref="BomItem"/>.
/// </remarks>
public sealed class BillOfMaterials : AggregateRoot<Guid>
{
    private readonly List<BomItem> _items = [];

    /// <summary>
    /// The finished product this BOM defines the recipe for.
    /// </summary>
    public MaterialCode ProductCode { get; private set; }

    /// <summary>
    /// Sequential version number within the product's BOM history.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Current lifecycle status of this BOM version.
    /// </summary>
    public BomStatus Status { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// The input materials required to produce the finished product.
    /// </summary>
    public IReadOnlyCollection<BomItem> Items => _items.AsReadOnly();

    private BillOfMaterials() : base(Guid.Empty)
    {
        ProductCode = default!;
    }

    private BillOfMaterials(Guid id, MaterialCode productCode, int version) : base(id)
    {
        ProductCode = productCode;
        Version = version;
        Status = BomStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to create a new BOM draft for the given product and version.
    /// </summary>
    public static BillOfMaterials Create(string productCode, int version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        if (version < 1)
            throw new ArgumentException("Version must be a positive integer.", nameof(version));

        return new BillOfMaterials(Guid.NewGuid(), MaterialCode.From(productCode), version);
    }

    /// <summary>
    /// Adds an input material item to this BOM draft.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the BOM is not in <see cref="BomStatus.Draft"/> status.</exception>
    public BomItem AddItem(string materialCode, decimal quantity, string unitOfMeasure)
    {
        if (Status != BomStatus.Draft)
            throw new InvalidOperationException($"Cannot add items to a BOM in status '{Status}'. Only Draft BOMs can be modified.");

        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);

        if (quantity <= 0)
            throw new ArgumentException("Item quantity must be greater than zero.", nameof(quantity));

        var normalizedCode = materialCode.Trim().ToUpperInvariant();

        if (_items.Any(i => i.MaterialCode.Value == normalizedCode))
            throw new InvalidOperationException($"Material '{normalizedCode}' already exists in this BOM. Remove the existing item before adding it again.");

        var item = BomItem.Create(Id, MaterialCode.From(normalizedCode), quantity, unitOfMeasure);
        _items.Add(item);
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }

    /// <summary>
    /// Activates this BOM version, making it the authoritative recipe for the product.
    /// The caller is responsible for reverting any previously active version to Draft
    /// before or after calling this method (enforced at use case level).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the BOM has no items, or is not in <see cref="BomStatus.Draft"/> status.
    /// </exception>
    public void Activate()
    {
        if (Status != BomStatus.Draft)
            throw new InvalidOperationException($"Cannot activate a BOM in status '{Status}'. Only Draft BOMs can be activated.");

        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot activate a BOM with no items. Add at least one material.");

        Status = BomStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Reverts this BOM version to Draft, used when a newer version is being activated.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the BOM is not in <see cref="BomStatus.Active"/> status.</exception>
    public void RevertToDraft()
    {
        if (Status != BomStatus.Active)
            throw new InvalidOperationException($"Cannot revert a BOM in status '{Status}'. Only Active BOMs can be reverted.");

        Status = BomStatus.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents a single input material line in a Bill of Materials.
/// </summary>
public sealed class BomItem : Entity<Guid>
{
    /// <summary>
    /// The BOM this item belongs to.
    /// </summary>
    public Guid BomId { get; private set; }

    /// <summary>
    /// The material code of the required input.
    /// </summary>
    public MaterialCode MaterialCode { get; private set; }

    /// <summary>
    /// The required quantity of the material.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// The unit of measure for the required quantity.
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    private BomItem() : base(Guid.Empty)
    {
        MaterialCode = default!;
        UnitOfMeasure = string.Empty;
    }

    private BomItem(Guid id, Guid bomId, MaterialCode materialCode, decimal quantity, string unitOfMeasure) : base(id)
    {
        BomId = bomId;
        MaterialCode = materialCode;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
    }

    internal static BomItem Create(Guid bomId, MaterialCode materialCode, decimal quantity, string unitOfMeasure)
    {
        return new BomItem(Guid.NewGuid(), bomId, materialCode, quantity, unitOfMeasure.Trim().ToUpperInvariant());
    }
}

/// <summary>
/// Represents the lifecycle status of a Bill of Materials version.
/// </summary>
public enum BomStatus
{
    /// <summary>
    /// Being defined; can be modified and is not yet in use.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The authoritative recipe for the product; used when releasing Production Orders.
    /// </summary>
    Active = 1
}
