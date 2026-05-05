namespace Microsoft.Extensions.Hosting;

public sealed class TenantRoutingOptions
{
    public int CatalogCacheTtlSeconds { get; init; } = 60;
    public string? DefaultTenantCode { get; init; }
    public string? ServiceKey { get; init; }
}

public interface ITenantContextAccessor
{
    RailFactory.BuildingBlocks.Tenancy.TenantContext? Current { get; set; }
}

public interface ITenantCatalogClient
{
    Task<TenantResolutionResult> ResolveAsync(string tenantCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantResolutionResult>> ListAllAsync(CancellationToken cancellationToken);
}

public interface ITenantConnectionResolver
{
    string ResolveConnection(string serviceKey);
}

public sealed record TenantResolutionResult(
    bool Found,
    string Code,
    string Locale,
    string TimeZone,
    bool IsActive,
    IReadOnlyDictionary<string, string>? ConnectionStrings = null)
{
    public IReadOnlyDictionary<string, string> ConnectionStrings { get; init; } = ConnectionStrings ?? new Dictionary<string, string>();

    public static TenantResolutionResult NotFound { get; } =
        new(false, string.Empty, string.Empty, string.Empty, false, new Dictionary<string, string>());
}

public sealed record TenantInfoDto(
    string Code,
    string Locale,
    string TimeZone,
    IReadOnlyDictionary<string, string> ConnectionStrings);
