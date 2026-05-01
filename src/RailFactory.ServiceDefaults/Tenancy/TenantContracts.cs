namespace Microsoft.Extensions.Hosting;

public sealed class TenantRoutingOptions
{
    public int CatalogCacheTtlSeconds { get; init; } = 60;
}

public interface ITenantContextAccessor
{
    RailFactory.BuildingBlocks.Tenancy.TenantContext? Current { get; set; }
}

public interface ITenantCatalogClient
{
    Task<TenantResolutionResult> ResolveAsync(string tenantCode, CancellationToken cancellationToken);
}

public sealed record TenantResolutionResult(
    bool Found,
    string Code,
    string Locale,
    string TimeZone,
    bool IsActive)
{
    public static TenantResolutionResult NotFound { get; } =
        new(false, string.Empty, string.Empty, string.Empty, false);
}

public sealed record TenantInfoDto(
    string Code,
    string Locale,
    string TimeZone);
