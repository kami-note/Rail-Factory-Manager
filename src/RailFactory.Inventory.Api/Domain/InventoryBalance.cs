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
