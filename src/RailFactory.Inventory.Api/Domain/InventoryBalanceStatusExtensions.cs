using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// Provides display-specific extensions for the <see cref="InventoryBalanceStatus"/> enum.
/// </summary>
public static class InventoryBalanceStatusExtensions
{
    /// <summary>
    /// Converts an inventory status into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    /// <param name="status">The balance status to convert.</param>
    /// <returns>A standardized display metadata object.</returns>
    public static DisplayStatus ToDisplayStatus(this InventoryBalanceStatus status) => status switch
    {
        InventoryBalanceStatus.Pending => new DisplayStatus("Pending", "Pendente", "warning"),
        InventoryBalanceStatus.Available => new DisplayStatus("Available", "Disponível", "success"),
        InventoryBalanceStatus.Blocked => new DisplayStatus("Blocked", "Bloqueado", "error"),
        InventoryBalanceStatus.Reserved => new DisplayStatus("Reserved", "Reservado", "info"),
        _ => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };

    /// <summary>
    /// Converts a material status into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this MaterialStatus status) => status switch
    {
        MaterialStatus.Draft => new DisplayStatus("Draft", "Rascunho", "default"),
        MaterialStatus.Verified => new DisplayStatus("Verified", "Verificado", "success"),
        _ => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };

    /// <summary>
    /// Converts an inventory source type into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this InventorySourceType type) => type switch
    {
        InventorySourceType.Purchase => new DisplayStatus("Purchase", "Compra/Recebimento", "default"),
        InventorySourceType.Production => new DisplayStatus("Production", "Produção Interna", "default"),
        InventorySourceType.Adjustment => new DisplayStatus("Adjustment", "Ajuste Manual", "default"),
        _ => new DisplayStatus(type.ToString(), type.ToString(), "default")
    };

    /// <summary>
    /// Converts a material category into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this MaterialCategory category) => category switch
    {
        MaterialCategory.RawMaterial => new DisplayStatus("RawMaterial", "Matéria-Prima", "default"),
        MaterialCategory.FinishedGood => new DisplayStatus("FinishedGood", "Produto Acabado", "default"),
        MaterialCategory.Packaging => new DisplayStatus("Packaging", "Embalagem", "default"),
        MaterialCategory.Consumable => new DisplayStatus("Consumable", "Consumível", "default"),
        _ => new DisplayStatus(category.ToString(), category.ToString(), "default")
    };
}
