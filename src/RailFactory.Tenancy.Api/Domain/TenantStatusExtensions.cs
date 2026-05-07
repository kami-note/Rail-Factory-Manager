using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Tenancy.Api.Domain;

/// <summary>
/// Provides display-specific extensions for the <see cref="TenantStatus"/> enum.
/// </summary>
public static class TenantStatusExtensions
{
    /// <summary>
    /// Converts a tenant status into a UI-friendly <see cref="DisplayStatus"/>.
    /// </summary>
    /// <param name="status">The tenant status to convert.</param>
    /// <returns>A standardized display metadata object.</returns>
    public static DisplayStatus ToDisplayStatus(this TenantStatus status) => status switch
    {
        TenantStatus.Active => new DisplayStatus("Active", "Ativo", "success"),
        TenantStatus.Disabled => new DisplayStatus("Disabled", "Inativo", "error"),
        _ => new DisplayStatus(status.ToString(), status.ToString(), "default")
    };
}
