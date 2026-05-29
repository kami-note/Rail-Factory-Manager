using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Represents the stock quantity of a specific material in a location.
/// </summary>
public sealed class InventoryBalance : AggregateRoot<Guid>
{
    /// <summary>
    /// The material SKU.
    /// </summary>
    public string MaterialCode { get; private set; }

    /// <summary>
    /// Base unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// Current quantity in stock.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// The physical or logical location of this balance.
    /// </summary>
    public Guid StockLocationId { get; private set; }

    /// <summary>
    /// External reference to the source of this balance (e.g., ReceiptId:ItemId).
    /// </summary>
    public string SourceReference { get; private set; }

    /// <summary>
    /// Optional lot number for traceability.
    /// </summary>
    public string? LotNumber { get; private set; }

    /// <summary>
    /// Optional expiration date.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; private set; }

    /// <summary>
    /// The origin of this balance.
    /// </summary>
    public InventorySourceType SourceType { get; private set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public InventoryBalanceStatus Status { get; private set; }

    /// <summary>
    /// Extended metadata in JSON format.
    /// </summary>
    public string? SourceMetadata { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// When reserved, references the Production Order that holds this stock.
    /// </summary>
    public Guid? ReservedForOrderId { get; private set; }

    private InventoryBalance() : base(Guid.Empty)
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
        InventoryBalanceStatus status,
        string? sourceMetadata) : base(id)
    {
        MaterialCode = materialCode;
        UnitOfMeasure = unitOfMeasure;
        Quantity = quantity;
        StockLocationId = stockLocationId;
        SourceReference = sourceReference;
        LotNumber = lotNumber;
        ExpirationDate = expirationDate;
        SourceType = sourceType;
        Status = status;
        SourceMetadata = sourceMetadata;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method for creating a pending balance from an external source.
    /// </summary>
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
        return new InventoryBalance(
            Guid.NewGuid(),
            materialCode,
            unitOfMeasure,
            quantity,
            stockLocationId,
            sourceReference,
            lotNumber,
            expirationDate,
            sourceType,
            InventoryBalanceStatus.Pending,
            sourceMetadata);
    }

    /// <summary>
    /// Reserves this balance for a Production Order, blocking it from other uses.
    /// </summary>
    /// <param name="productionOrderId">The Production Order claiming this stock.</param>
    /// <param name="requiredQuantity">The quantity required by the order. Must be less than or equal to the balance quantity.</param>
    /// <exception cref="InvalidOperationException">Thrown when the balance is not Available or has insufficient quantity.</exception>
    public void Reserve(Guid productionOrderId, decimal requiredQuantity)
    {
        if (Status != InventoryBalanceStatus.Available)
            throw new InvalidOperationException($"Cannot reserve balance in status '{Status}'. Only 'Available' balances can be reserved.");

        if (requiredQuantity > Quantity)
            throw new InvalidOperationException($"Insufficient stock: required {requiredQuantity}, available {Quantity}.");

        ReservedForOrderId = productionOrderId;
        Status = InventoryBalanceStatus.Reserved;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Records the actual consumption of a reserved balance by a Production Order.
    /// Reduces quantity by the consumed amount and releases any remaining stock back to Available.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the balance is not Reserved.</exception>
    public void Consume(decimal consumedQuantity)
    {
        if (Status != InventoryBalanceStatus.Reserved)
            throw new InvalidOperationException($"Cannot consume balance in status '{Status}'. Only 'Reserved' balances can be consumed.");

        if (consumedQuantity < 0)
            throw new ArgumentException("Consumed quantity cannot be negative.", nameof(consumedQuantity));

        Quantity = consumedQuantity;
        ReservedForOrderId = null;

        // Remaining stock (if any) returns to Available after actual consumption is recorded separately.
        Status = InventoryBalanceStatus.Available;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Releases a reservation without consuming the stock (e.g., cancelled Production Order).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the balance is not Reserved.</exception>
    public void ReleaseReservation()
    {
        if (Status != InventoryBalanceStatus.Reserved)
            throw new InvalidOperationException($"Cannot release reservation for balance in status '{Status}'.");

        ReservedForOrderId = null;
        Status = InventoryBalanceStatus.Available;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Debits (reduces) this Available balance by the dispatched quantity.
    /// Used when a logistics dispatch ships stock out of the warehouse.
    /// </summary>
    public void Debit(decimal quantity)
    {
        if (Status != InventoryBalanceStatus.Available)
            throw new InvalidOperationException($"Cannot debit balance in status '{Status}'. Only 'Available' balances can be debited.");

        if (quantity <= 0)
            throw new ArgumentException("Debit quantity must be positive.", nameof(quantity));

        if (quantity > Quantity)
            throw new InvalidOperationException($"Cannot debit {quantity} from balance with only {Quantity} available.");

        Quantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Confirms the physical count for a pending balance, making it available or blocked.
    /// </summary>
    public void Confirm(decimal quantity, string? lotNumber, DateTimeOffset? expirationDate, bool isApproved)
    {
        // ELITE FIX: Rigid state machine guard
        if (Status != InventoryBalanceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm balance in status '{Status}'. Only 'Pending' balances can be confirmed.");
        }

        if (quantity < 0)
        {
            throw new ArgumentException("Counted quantity cannot be negative.");
        }

        Quantity = quantity;
        LotNumber = lotNumber?.Trim();
        ExpirationDate = expirationDate;
        Status = isApproved ? InventoryBalanceStatus.Available : InventoryBalanceStatus.Blocked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
