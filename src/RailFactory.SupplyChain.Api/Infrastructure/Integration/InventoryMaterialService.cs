using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed class InventoryMaterialService(
    IHttpClientFactory httpClientFactory,
    ITenantContextAccessor tenantContext,
    IConfiguration configuration,
    IMemoryCache cache) : IInventoryMaterialService
{
    private const string ClientName = "inventory-integration";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IDictionary<string, MaterialMetadata>> GetMaterialsByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken)
    {
        if (!materialCodes.Any())
        {
            return new Dictionary<string, MaterialMetadata>();
        }

        var tenantCode = tenantContext.Current?.TenantCode;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var results = new Dictionary<string, MaterialMetadata>();
        var missingCodes = new List<string>();

        foreach (var code in materialCodes.Distinct())
        {
            var cacheKey = $"material_meta:{tenantCode}:{code}";
            if (cache.TryGetValue(cacheKey, out MaterialMetadata? cached))
            {
                results[code] = cached!;
            }
            else
            {
                missingCodes.Add(code);
            }
        }

        if (missingCodes.Count == 0)
        {
            return results;
        }

        var client = httpClientFactory.CreateClient(ClientName);
        var apiKey = configuration["InternalApiKey"];
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "/internal/materials");
        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Internal-Key", apiKey);
        }
        request.Content = JsonContent.Create(new { MaterialCodes = missingCodes });

        using var response = await client.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            // Fail gracefully to avoid breaking the UI
            return results;
        }

        var fetchedData = await response.Content.ReadFromJsonAsync<List<MaterialMetadata>>(cancellationToken: cancellationToken);
        
        if (fetchedData != null)
        {
            foreach (var meta in fetchedData)
            {
                var cacheKey = $"material_meta:{tenantCode}:{meta.MaterialCode}";
                cache.Set(cacheKey, meta, CacheDuration);
                results[meta.MaterialCode] = meta;
            }
        }
        
        return results;
    }

    public async Task<MaterialMetadata> CreateMaterialAsync(CreateMaterialInput input, CancellationToken cancellationToken)
    {
        var tenantCode = tenantContext.Current?.TenantCode;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var client = httpClientFactory.CreateClient(ClientName);
        var apiKey = configuration["InternalApiKey"];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/materials");
        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Internal-Key", apiKey);
        }
        request.Content = JsonContent.Create(input);

        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            ProblemDetails? problem = null;
            try 
            {
                problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            }
            catch { /* Ignore parse error and fallback to status code */ }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Material already exists, try to fetch its metadata to maintain idempotency
                var existing = await GetMaterialsByCodesAsync(new[] { input.MaterialCode }, cancellationToken);
                if (existing.TryGetValue(input.MaterialCode, out var meta))
                {
                    return meta;
                }

                throw new RemoteServiceConflictException(
                    problem?.Code ?? "inventory.conflict", 
                    problem?.Detail ?? $"Material '{input.MaterialCode}' already exists but metadata could not be retrieved.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new RemoteServiceValidationException(
                    problem?.Code ?? "inventory.validation_error", 
                    problem?.Detail ?? "Downstream service validation failed.");
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create material in Inventory. Status: {response.StatusCode}. Error: {error}");
        }

        // The Inventory API returns MaterialResponse, but we only need MaterialMetadata
        var created = await response.Content.ReadFromJsonAsync<MaterialMetadata>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created material metadata.");

        // Cache the newly created material
        var cacheKey = $"material_meta:{tenantCode}:{created.MaterialCode}";
        cache.Set(cacheKey, created, CacheDuration);

        return created;
    }

    private sealed record ProblemDetails(string? Title, string? Detail, string? Code);
}
