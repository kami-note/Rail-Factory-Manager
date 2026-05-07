using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Represents a stock balance for a specific material in a location.
/// </summary>
/// <remarks>
/// This entity follows a hybrid relational-document model for traceability.
/// Operational fields (Lot, Expiry) are first-class columns, while
/// source-specific data (Purchase vs Production) are encapsulated in JSON metadata.
/// </remarks>
public sealed class InventoryBalance : AggregateRoot<Guid>
{
    /// <summary>
    /// Unique code for the material (SKU).
    /// </summary>
    public string MaterialCode { get; private set; }

    /// <summary>
    /// Unit of measurement (e.g., UN, KG).
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// Current quantity in stock.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Current availability status of the balance.
    /// </summary>
    public InventoryBalanceStatus Status { get; private set; }

    /// <summary>
    /// Reference to the physical or logical stock location.
    /// </summary>
    public Guid StockLocationId { get; private set; }

    /// <summary>
    /// Unique reference to the source transaction (e.g., ReceiptId:ItemId or WorkOrderId).
    /// </summary>
    public string SourceReference { get; private set; }

    /// <summary>
    /// Tracking lot or batch number.
    /// </summary>
    public string? LotNumber { get; private set; }

    /// <summary>
    /// Optional expiration date for the material.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; private set; }

    /// <summary>
    /// The origin type of this balance (e.g., Purchase, Production).
    /// </summary>
    public InventorySourceType SourceType { get; private set; }

    /// <summary>
    /// Rich traceability metadata stored as JSON (e.g., NF-e keys, OP details).
    /// </summary>
    public string? SourceMetadata { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private InventoryBalance()
        : base(Guid.Empty)
    {
        MaterialCode = string.Empty;
        UnitOfMeasure = string.Empty;
        SourceReference = string.Empty;
    }

    private InventoryBalance(
        Guid id,
        string materialCode,
        string unitOfMeasure,
        decimal quantity,
        Guid stockLocationId,
        string sourceReference,
        string? lotNumber,
        DateTimeOffset? expirationDate,
        InventorySourceType sourceType,
        string? sourceMetadata)
        : base(id)
    {
        MaterialCode = materialCode;
        UnitOfMeasure = unitOfMeasure;
        Quantity = quantity;
        Status = InventoryBalanceStatus.Pending;
        StockLocationId = stockLocationId;
        SourceReference = sourceReference;
        LotNumber = lotNumber;
        ExpirationDate = expirationDate;
        SourceType = sourceType;
        SourceMetadata = sourceMetadata;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method for creating a new pending balance from an external integration.
    /// </summary>
    /// <param name="materialCode">Material SKU.</param>
    /// <param name="unitOfMeasure">Unit of measure.</param>
    /// <param name="quantity">Initial quantity.</param>
    /// <param name="stockLocationId">Destination location.</param>
    /// <param name="sourceReference">Unique source identifier.</param>
    /// <param name="lotNumber">Optional tracking lot.</param>
    /// <param name="expirationDate">Optional expiration date.</param>
    /// <param name="sourceType">Type of origin.</param>
    /// <param name="sourceMetadata">Optional rich metadata in JSON format.</param>
    /// <returns>A new <see cref="InventoryBalance"/> in Pending status.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when quantity is not positive.</exception>
    public static InventoryBalance CreatePending(
        string materialCode,
        string unitOfMeasure,
        decimal quantity,
        Guid stockLocationId,
        string sourceReference,
        string? lotNumber,
        DateTimeOffset? expirationDate,
        InventorySourceType sourceType,
        string? sourceMetadata)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        return new InventoryBalance(
            Guid.NewGuid(),
            materialCode.Trim(),
            unitOfMeasure.Trim(),
            quantity,
            stockLocationId,
            sourceReference.Trim(),
            lotNumber?.Trim(),
            expirationDate,
            sourceType,
            sourceMetadata);
    }

    /// <summary>
    /// Confirms a pending balance with actual conference data.
    /// </summary>
    /// <param name="quantity">Actual quantity counted.</param>
    /// <param name="lotNumber">Tracking lot confirmed.</param>
    /// <param name="expirationDate">Expiration date confirmed.</param>
    /// <param name="isApproved">Whether the balance is approved for use.</param>
    /// <exception cref="InvalidOperationException">Thrown if balance is not Pending.</exception>
    public void Confirm(decimal quantity, string? lotNumber, DateTimeOffset? expirationDate, bool isApproved)
    {
        if (Status != InventoryBalanceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm balance in status '{Status}'. Only 'Pending' balances can be confirmed.");
        }

        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Confirmed quantity cannot be negative.");
        }

        Quantity = quantity;
        LotNumber = lotNumber?.Trim();
        ExpirationDate = expirationDate;
        Status = isApproved ? InventoryBalanceStatus.Available : InventoryBalanceStatus.Blocked;
    }
}
