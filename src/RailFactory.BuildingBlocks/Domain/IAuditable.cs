using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.BuildingBlocks.Domain;

/// <summary>
/// Defines the contract for entities that require automated audit trails.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// The identity of the actor who created this record.
    /// </summary>
    EmailAddress CreatedBy { get; }

    /// <summary>
    /// The identity of the actor who last modified this record.
    /// </summary>
    EmailAddress LastModifiedBy { get; }
}
