namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Defines the origin of an inventory balance.
/// </summary>
public enum InventorySourceType
{
    /// <summary>
    /// Material received via purchase (Inbound/Supply Chain).
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// Material produced internally (Finished Goods).
    /// </summary>
    Production = 2,

    /// <summary>
    /// Initial stock or manual adjustment.
    /// </summary>
    Adjustment = 3
}
