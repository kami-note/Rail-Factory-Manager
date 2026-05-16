using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Records material scrapped (discarded as waste) during Production Order execution.
/// </summary>
public sealed class ScrapRecord : Entity<Guid>
{
    /// <summary>
    /// The Production Order during which the scrap occurred.
    /// </summary>
    public Guid ProductionOrderId { get; private set; }

    /// <summary>
    /// The material that was scrapped.
    /// </summary>
    public MaterialCode MaterialCode { get; private set; }

    /// <summary>
    /// The scrapped quantity.
    /// </summary>
    public decimal ScrapQuantity { get; private set; }

    /// <summary>
    /// The unit of measure for the scrapped quantity.
    /// </summary>
    public string UnitOfMeasure { get; private set; }

    /// <summary>
    /// The mandatory reason for the scrap event.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Timestamp of the scrap recording.
    /// </summary>
    public DateTimeOffset RecordedAt { get; private set; }

    private ScrapRecord() : base(Guid.Empty)
    {
        MaterialCode = default!;
        UnitOfMeasure = string.Empty;
        Reason = string.Empty;
    }

    private ScrapRecord(Guid id, Guid productionOrderId, MaterialCode materialCode, decimal scrapQuantity, string unitOfMeasure, string reason) : base(id)
    {
        ProductionOrderId = productionOrderId;
        MaterialCode = materialCode;
        ScrapQuantity = scrapQuantity;
        UnitOfMeasure = unitOfMeasure;
        Reason = reason;
        RecordedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method for recording a scrap event.
    /// </summary>
    public static ScrapRecord Create(Guid productionOrderId, string materialCode, decimal scrapQuantity, string unitOfMeasure, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (scrapQuantity <= 0)
            throw new ArgumentException("Scrap quantity must be greater than zero.", nameof(scrapQuantity));

        return new ScrapRecord(Guid.NewGuid(), productionOrderId, MaterialCode.From(materialCode), scrapQuantity, unitOfMeasure.Trim().ToUpperInvariant(), reason.Trim());
    }
}
