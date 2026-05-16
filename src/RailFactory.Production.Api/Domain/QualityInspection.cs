using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Records the quality inspection result for a Production Order before it can be completed.
/// </summary>
/// <remarks>
/// Invariant: A Production Order requires at least one Passed inspection before it can transition to Completed.
/// </remarks>
public sealed class QualityInspection : Entity<Guid>
{
    /// <summary>
    /// The Production Order this inspection belongs to.
    /// </summary>
    public Guid ProductionOrderId { get; private set; }

    /// <summary>
    /// The result of the inspection.
    /// </summary>
    public InspectionResult Result { get; private set; }

    /// <summary>
    /// The operator or quality engineer who performed the inspection.
    /// </summary>
    public string InspectedBy { get; private set; }

    /// <summary>
    /// Optional notes from the inspector.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Timestamp of the inspection.
    /// </summary>
    public DateTimeOffset InspectedAt { get; private set; }

    private QualityInspection() : base(Guid.Empty)
    {
        InspectedBy = string.Empty;
    }

    private QualityInspection(Guid id, Guid productionOrderId, InspectionResult result, string inspectedBy, string? notes) : base(id)
    {
        ProductionOrderId = productionOrderId;
        Result = result;
        InspectedBy = inspectedBy;
        Notes = notes;
        InspectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory method for recording an inspection result.
    /// </summary>
    public static QualityInspection Create(Guid productionOrderId, InspectionResult result, string inspectedBy, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inspectedBy);
        return new QualityInspection(Guid.NewGuid(), productionOrderId, result, inspectedBy.Trim(), notes?.Trim());
    }
}

/// <summary>
/// The outcome of a quality inspection.
/// </summary>
public enum InspectionResult
{
    /// <summary>
    /// Production meets quality standards. Order can be completed.
    /// </summary>
    Passed = 0,

    /// <summary>
    /// Production failed quality standards. Order cannot be completed until re-inspected.
    /// </summary>
    Failed = 1
}
