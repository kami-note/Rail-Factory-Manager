namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Defines the strategic sourcing origin of a material.
/// </summary>
public enum ProcurementType
{
    /// <summary>
    /// Material is manufactured internally (requires a BOM / Recipe).
    /// </summary>
    Make = 0,

    /// <summary>
    /// Material is purchased from external suppliers.
    /// </summary>
    Buy = 1,

    /// <summary>
    /// Material can be both manufactured internally or purchased externally.
    /// </summary>
    MakeAndBuy = 2
}
