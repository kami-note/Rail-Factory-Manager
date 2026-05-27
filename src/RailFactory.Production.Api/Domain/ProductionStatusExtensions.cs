using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Production.Api.Domain;

/// <summary>
/// Provides display-specific extensions for Production domain status enums.
/// Follows the BFF-Driven Status protocol: all statuses are converted to a
/// <see cref="DisplayStatus"/> object so the UI renders labels and colors
/// without hardcoding them in components.
/// </summary>
public static class ProductionStatusExtensions
{
    /// <summary>
    /// Converts a <see cref="WorkCenterStatus"/> into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this WorkCenterStatus status) => status switch
    {
        WorkCenterStatus.Active   => new DisplayStatus("Active",   "Ativo",    "success"),
        WorkCenterStatus.Inactive => new DisplayStatus("Inactive", "Inativo",  "default"),
        _                         => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };

    /// <summary>
    /// Converts a <see cref="BomStatus"/> into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this BomStatus status) => status switch
    {
        BomStatus.Draft  => new DisplayStatus("Draft",  "Rascunho", "default"),
        BomStatus.Active => new DisplayStatus("Active", "Ativo",    "success"),
        _                => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };

    /// <summary>
    /// Converts a <see cref="ProductionOrderStatus"/> into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    public static DisplayStatus ToDisplayStatus(this ProductionOrderStatus status) => status switch
    {
        ProductionOrderStatus.Draft       => new DisplayStatus("Draft",       "Rascunho",    "default"),
        ProductionOrderStatus.Released    => new DisplayStatus("Released",    "Liberada",    "info"),
        ProductionOrderStatus.InExecution => new DisplayStatus("InExecution", "Em Execução", "warning"),
        ProductionOrderStatus.Completed   => new DisplayStatus("Completed",   "Concluída",   "success"),
        ProductionOrderStatus.Cancelled   => new DisplayStatus("Cancelled",   "Cancelada",   "error"),
        _                                 => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };
}
