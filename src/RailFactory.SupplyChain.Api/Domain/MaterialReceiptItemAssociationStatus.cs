namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Tracks the operator decision state for supplier SKU to internal material SKU resolution.
/// </summary>
public enum MaterialReceiptItemAssociationStatus
{
    Pending = 0,
    Mapped = 1,
    CreatedAndMapped = 2,
    ReviewLater = 3,
    Ignored = 4,
    Conflict = 5
}
