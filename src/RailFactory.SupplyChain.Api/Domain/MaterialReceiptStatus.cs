namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Defines the lifecycle states of a material receipt.
/// </summary>
public enum MaterialReceiptStatus
{
    /// <summary>
    /// Initial state, receipt is being drafted.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Receipt is registered and waiting for conference.
    /// </summary>
    Registered = 1,

    /// <summary>
    /// Material is currently being counted.
    /// </summary>
    InConference = 2,

    /// <summary>
    /// Conference completed with no divergences.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Conference completed with divergences.
    /// </summary>
    Divergent = 4,

    /// <summary>
    /// Receipt was cancelled.
    /// </summary>
    Cancelled = 5
}
