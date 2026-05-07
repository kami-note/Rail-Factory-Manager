using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Provides display-specific extensions for the <see cref="MaterialReceiptStatus"/> enum.
/// </summary>
public static class MaterialReceiptStatusExtensions
{
    /// <summary>
    /// Converts a domain status into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    /// <param name="status">The receipt status to convert.</param>
    /// <returns>A standardized display metadata object.</returns>
    public static DisplayStatus ToDisplayStatus(this MaterialReceiptStatus status) => status switch
    {
        MaterialReceiptStatus.Draft => new DisplayStatus("Draft", "Rascunho", "default"),
        MaterialReceiptStatus.Registered => new DisplayStatus("Registered", "Registrado", "info"),
        MaterialReceiptStatus.InConference => new DisplayStatus("InConference", "Em Conferência", "warning"),
        MaterialReceiptStatus.Approved => new DisplayStatus("Approved", "Conferido", "success"),
        MaterialReceiptStatus.Divergent => new DisplayStatus("Divergent", "Divergente", "error"),
        MaterialReceiptStatus.Cancelled => new DisplayStatus("Cancelled", "Cancelado", "error"),
        _ => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };
}
