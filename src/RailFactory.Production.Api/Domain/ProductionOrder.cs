using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Represents a manufacturing order to produce a quantity of a finished product.
/// </summary>
/// <remarks>
/// State machine: Draft → Released → InExecution → Completed | Cancelled.
/// Guards:
/// - <see cref="Release"/>: requires an Active BOM and an Active Work Center (validated at use case level).
/// - <see cref="Cancel"/>: permitted in Draft and Released; blocked in InExecution and Completed.
/// </remarks>
public sealed class ProductionOrder : AggregateRoot<Guid>
{
    /// <summary>
    /// Human-readable sequential identifier (e.g., OP-2026-0001).
    /// </summary>
    public string OrderNumber { get; private set; }

    /// <summary>
    /// The finished product to be manufactured.
    /// </summary>
    public MaterialCode ProductCode { get; private set; }

    /// <summary>
    /// The BOM version used to define the required input materials.
    /// </summary>
    public Guid BomId { get; private set; }

    /// <summary>
    /// The Work Center responsible for executing this order.
    /// </summary>
    public Guid WorkCenterId { get; private set; }

    /// <summary>
    /// The target quantity to be produced.
    /// </summary>
    public decimal PlannedQuantity { get; private set; }

    /// <summary>
    /// Current lifecycle status of the Production Order.
    /// </summary>
    public ProductionOrderStatus Status { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private ProductionOrder() : base(Guid.Empty)
    {
        OrderNumber = string.Empty;
        ProductCode = default!;
    }

    private ProductionOrder(
        Guid id,
        string orderNumber,
        MaterialCode productCode,
        Guid bomId,
        Guid workCenterId,
        decimal plannedQuantity) : base(id)
    {
        OrderNumber = orderNumber;
        ProductCode = productCode;
        BomId = bomId;
        WorkCenterId = workCenterId;
        PlannedQuantity = plannedQuantity;
        Status = ProductionOrderStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to create a new Production Order in Draft status.
    /// </summary>
    public static ProductionOrder Create(
        string orderNumber,
        string productCode,
        Guid bomId,
        Guid workCenterId,
        decimal plannedQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        if (bomId == Guid.Empty)
            throw new ArgumentException("A valid BOM must be specified.", nameof(bomId));

        if (workCenterId == Guid.Empty)
            throw new ArgumentException("A valid Work Center must be specified.", nameof(workCenterId));

        if (plannedQuantity <= 0)
            throw new ArgumentException("Planned quantity must be greater than zero.", nameof(plannedQuantity));

        return new ProductionOrder(
            Guid.NewGuid(),
            orderNumber.Trim().ToUpperInvariant(),
            MaterialCode.From(productCode),
            bomId,
            workCenterId,
            plannedQuantity);
    }

    /// <summary>
    /// Transitions the order to Released, authorizing execution on the shop floor.
    /// The caller must validate that the referenced BOM is Active and the Work Center is Active.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the order is not in <see cref="ProductionOrderStatus.Draft"/> status.</exception>
    public void Release()
    {
        if (Status != ProductionOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot release a Production Order in status '{Status}'. Only Draft orders can be released.");

        Status = ProductionOrderStatus.Released;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Transitions the order to InExecution once the operator starts work on the shop floor.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the order is not in <see cref="ProductionOrderStatus.Released"/> status.</exception>
    public void StartExecution()
    {
        if (Status != ProductionOrderStatus.Released)
            throw new InvalidOperationException($"Cannot start execution for a Production Order in status '{Status}'. Only Released orders can be started.");

        Status = ProductionOrderStatus.InExecution;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Completes the Production Order. Requires a passed quality inspection.
    /// </summary>
    /// <param name="inspectionPassed">Whether the quality inspection was approved.</param>
    /// <exception cref="InvalidOperationException">Thrown when the order is not InExecution or inspection failed.</exception>
    public void Complete(bool inspectionPassed)
    {
        if (Status != ProductionOrderStatus.InExecution)
            throw new InvalidOperationException($"Cannot complete a Production Order in status '{Status}'. Only InExecution orders can be completed.");

        if (!inspectionPassed)
            throw new InvalidOperationException("Cannot complete Production Order: quality inspection has not been approved.");

        Status = ProductionOrderStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cancels the Production Order. Not permitted once execution has started.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the order is in <see cref="ProductionOrderStatus.InExecution"/> or <see cref="ProductionOrderStatus.Completed"/>.
    /// </exception>
    public void Cancel()
    {
        if (Status is ProductionOrderStatus.InExecution or ProductionOrderStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel a Production Order in status '{Status}'.");

        if (Status == ProductionOrderStatus.Cancelled)
            throw new InvalidOperationException("Production Order is already cancelled.");

        Status = ProductionOrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents the lifecycle state of a Production Order.
/// </summary>
public enum ProductionOrderStatus
{
    /// <summary>
    /// Order is being defined and has not yet been authorized.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Order has been authorized and is awaiting execution on the shop floor.
    /// Stock reservation will be triggered by this transition (P5).
    /// </summary>
    Released = 1,

    /// <summary>
    /// Order is actively being executed on the shop floor.
    /// </summary>
    InExecution = 2,

    /// <summary>
    /// Order has been successfully completed and production recorded.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Order was cancelled before completion.
    /// </summary>
    Cancelled = 4
}
