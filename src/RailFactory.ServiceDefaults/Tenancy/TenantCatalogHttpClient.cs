using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RailFactory.BuildingBlocks.Presentation;

namespace Microsoft.Extensions.Hosting;

public sealed class TenantCatalogHttpClient(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<TenantRoutingOptions> options) : ITenantCatalogClient
{
    public async Task<TenantResolutionResult> ResolveAsync(string tenantCode, CancellationToken cancellationToken)
    {
        var normalizedCode = tenantCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return TenantResolutionResult.NotFound;
        }

        var cacheKey = $"tenant::{normalizedCode.ToLowerInvariant()}";
        if (cache.TryGetValue<TenantResolutionResult>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        using var response = await httpClient.GetAsync($"/tenants/{Uri.EscapeDataString(normalizedCode)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            cache.Set(cacheKey, TenantResolutionResult.NotFound, GetCacheLifetime(options.Value.CatalogCacheTtlSeconds));
            return TenantResolutionResult.NotFound;
        }

        response.EnsureSuccessStatusCode();

        var tenant = await response.Content.ReadFromJsonAsync<TenantCatalogResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Tenant catalog returned an empty response.");

        var resolved = new TenantResolutionResult(
            true,
            tenant.Code,
            tenant.Locale,
            tenant.TimeZone,
            string.Equals(tenant.Status?.Key, "Active", StringComparison.OrdinalIgnoreCase),
            tenant.ConnectionStrings);

        cache.Set(cacheKey, resolved, GetCacheLifetime(options.Value.CatalogCacheTtlSeconds));
        return resolved;
    }

    public async Task<IReadOnlyList<TenantResolutionResult>> ListAllAsync(CancellationToken cancellationToken)
    {
        // For simplicity and because this is used mainly by background workers, we don't cache the whole list
        using var response = await httpClient.GetAsync("/tenants", cancellationToken);
        response.EnsureSuccessStatusCode();

        var tenants = await response.Content.ReadFromJsonAsync<IReadOnlyList<TenantCatalogResponse>>(cancellationToken)
            ?? throw new InvalidOperationException("Tenant catalog returned an empty response.");

        return tenants.Select(tenant => new TenantResolutionResult(
            true,
            tenant.Code,
            tenant.Locale,
            tenant.TimeZone,
            string.Equals(tenant.Status?.Key, "Active", StringComparison.OrdinalIgnoreCase),
            tenant.ConnectionStrings)).ToList();

    }

    private static TimeSpan GetCacheLifetime(int ttlSeconds)
        => ttlSeconds > 0 ? TimeSpan.FromSeconds(ttlSeconds) : TimeSpan.FromSeconds(60);

    private sealed record TenantCatalogResponse(
        string Code,
        string Locale,
        string TimeZone,
        DisplayStatus Status,
        IReadOnlyDictionary<string, string> ConnectionStrings);
}
