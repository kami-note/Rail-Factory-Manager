using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Records the actual quantity of a material consumed during Production Order execution.
/// </summary>
public sealed class ConsumptionRecord : Entity<Guid>
{
    /// <summary>
    /// The Production Order this consumption belongs to.
    /// </summary>
    public Guid ProductionOrderId { get; private set; }

    /// <summary>
    /// The material that was consumed.
    /// </summary>
    public MaterialCode MaterialCode { get; private set; }

    /// <summary>
    /// The actual quantity consumed.
    /// </summary>
    public decimal ConsumedQuantity { get; private set; }

    /// <summary>
    /// The unit of measure for the consumed quantity.
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// The inventory balance from which the material was consumed.
    /// </summary>
    public Guid? InventoryBalanceId { get; private set; }

    /// <summary>
    /// Timestamp of the consumption recording.
    /// </summary>
    public DateTimeOffset RecordedAt { get; private set; }

    private ConsumptionRecord() : base(Guid.Empty)
    {
        MaterialCode = default!;
        UnitOfMeasure = string.Empty;
    }

    private ConsumptionRecord(Guid id, Guid productionOrderId, MaterialCode materialCode, decimal consumedQuantity, string unitOfMeasure, Guid? inventoryBalanceId) : base(id)
    {
        ProductionOrderId = productionOrderId;
        MaterialCode = materialCode;
        ConsumedQuantity = consumedQuantity;
        UnitOfMeasure = unitOfMeasure;
        InventoryBalanceId = inventoryBalanceId;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method for recording material consumption.
    /// </summary>
    public static ConsumptionRecord Create(Guid productionOrderId, string materialCode, decimal consumedQuantity, string unitOfMeasure, Guid? inventoryBalanceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);

        if (consumedQuantity <= 0)
            throw new ArgumentException("Consumed quantity must be greater than zero.", nameof(consumedQuantity));

        return new ConsumptionRecord(Guid.NewGuid(), productionOrderId, MaterialCode.From(materialCode), consumedQuantity, unitOfMeasure.Trim().ToUpperInvariant(), inventoryBalanceId);
    }
}
