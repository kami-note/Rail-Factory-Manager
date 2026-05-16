using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Represents a physical or logical production station where manufacturing operations are performed.
/// </summary>
/// <remarks>
/// Invariant: A WorkCenter cannot be deactivated while it has Production Orders in <see cref="ProductionOrderStatus.Released"/>
/// or <see cref="ProductionOrderStatus.InExecution"/> state. This guard is enforced at the use case level,
/// as the WorkCenter aggregate does not own the Production Order collection.
/// </remarks>
public sealed class WorkCenter : AggregateRoot<Guid>
{
    /// <summary>
    /// Unique business code that identifies the work center on the shop floor.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Human-readable name of the work center.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Current operational status.
    /// </summary>
    public WorkCenterStatus Status { get; private set; }

    /// <summary>
    /// Audit timestamp for record creation.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Audit timestamp for the last modification.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    private WorkCenter() : base(Guid.Empty)
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private WorkCenter(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        Status = WorkCenterStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory method to create a new active Work Center.
    /// </summary>
    public static WorkCenter Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new WorkCenter(Guid.NewGuid(), code.Trim().ToUpperInvariant(), name.Trim());
    }

    /// <summary>
    /// Transitions the Work Center to inactive. The caller is responsible for ensuring no active
    /// Production Orders are linked before invoking this method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the Work Center is already inactive.</exception>
    public void Deactivate()
    {
        if (Status == WorkCenterStatus.Inactive)
            throw new InvalidOperationException("Work Center is already inactive.");

        Status = WorkCenterStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents the operational state of a Work Center.
/// </summary>
public enum WorkCenterStatus
{
    /// <summary>
    /// Available to receive and process Production Orders.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Not available for new Production Orders.
    /// </summary>
    Inactive = 1
}
