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
    /// The standard batch size/yield quantity this BOM defines the recipe for.
    /// </summary>
    public decimal BatchSize { get; private set; }

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

    private BillOfMaterials(Guid id, MaterialCode productCode, int version, decimal batchSize = 1.0m) : base(id)
    {
        ProductCode = productCode;
        Version = version;
        Status = BomStatus.Draft;
        BatchSize = batchSize;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to create a new BOM draft for the given product, version, and batch size.
    /// </summary>
    public static BillOfMaterials Create(string productCode, int version, decimal batchSize = 1.0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        if (version < 1)
            throw new ArgumentException("Version must be a positive integer.", nameof(version));

        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));

        return new BillOfMaterials(Guid.NewGuid(), MaterialCode.From(productCode), version, batchSize);
    }

    /// <summary>
    /// Adds an input material item to this BOM draft.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the BOM is not in <see cref="BomStatus.Draft"/> status.</exception>
    public BomItem AddItem(string materialCode, decimal quantity, string unitOfMeasure, decimal scrapFactor = 0m)
    {
        if (Status != BomStatus.Draft)
            throw new InvalidOperationException($"Cannot add items to a BOM in status '{Status}'. Only Draft BOMs can be modified.");

        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);

        if (quantity <= 0)
            throw new ArgumentException("Item quantity must be greater than zero.", nameof(quantity));

        if (scrapFactor < 0 || scrapFactor >= 1)
            throw new ArgumentException("Scrap factor must be between 0 (inclusive) and 1 (exclusive).", nameof(scrapFactor));

        var normalizedCode = materialCode.Trim().ToUpperInvariant();

        if (_items.Any(i => i.MaterialCode.Value == normalizedCode))
            throw new InvalidOperationException($"Material '{normalizedCode}' already exists in this BOM. Remove the existing item before adding it again.");

        var item = BomItem.Create(Id, MaterialCode.From(normalizedCode), quantity, unitOfMeasure, scrapFactor);
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

    /// <summary>
    /// Creates a new draft BOM by cloning the items and batch size from this BOM version.
    /// </summary>
    /// <param name="newVersion">The version number for the cloned BOM.</param>
    /// <returns>A new <see cref="BillOfMaterials"/> draft containing copies of all items.</returns>
    /// <exception cref="ArgumentException">Thrown when the new version is less than or equal to the current version.</exception>
    public BillOfMaterials CloneAsDraft(int newVersion)
    {
        if (newVersion <= Version)
            throw new ArgumentException($"New version ({newVersion}) must be greater than current version ({Version}).", nameof(newVersion));

        var clone = new BillOfMaterials(Guid.NewGuid(), ProductCode, newVersion, BatchSize);

        foreach (var item in _items)
        {
            var clonedItem = BomItem.Create(clone.Id, item.MaterialCode, item.Quantity, item.UnitOfMeasure, item.ScrapFactor);
            clone._items.Add(clonedItem);
        }

        clone.UpdatedAt = DateTimeOffset.UtcNow;
        return clone;
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

    /// <summary>
    /// The expected percentage of technical scrap/loss during production (expressed as a decimal, e.g., 0.05 for 5%).
    /// </summary>
    public decimal ScrapFactor { get; private set; }

    private BomItem() : base(Guid.Empty)
    {
        MaterialCode = default!;
        UnitOfMeasure = string.Empty;
    }

    private BomItem(Guid id, Guid bomId, MaterialCode materialCode, decimal quantity, string unitOfMeasure, decimal scrapFactor) : base(id)
    {
        BomId = bomId;
        MaterialCode = materialCode;
        Quantity = quantity;
        UnitOfMeasure = unitOfMeasure;
        ScrapFactor = scrapFactor;
    }

    internal static BomItem Create(Guid bomId, MaterialCode materialCode, decimal quantity, string unitOfMeasure, decimal scrapFactor = 0m)
    {
        if (scrapFactor < 0 || scrapFactor >= 1)
            throw new ArgumentException("Scrap factor must be between 0 (inclusive) and 1 (exclusive).", nameof(scrapFactor));

        return new BomItem(Guid.NewGuid(), bomId, materialCode, quantity, unitOfMeasure.Trim().ToUpperInvariant(), scrapFactor);
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
