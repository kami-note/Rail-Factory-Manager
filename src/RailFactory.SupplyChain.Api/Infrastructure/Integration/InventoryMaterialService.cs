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
    private const string InternalMaterialsPath = "/api/inventory/internal/materials";
    private const string InternalCreateMaterialPath = "/api/inventory/internal/materials/create";
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

        var fetchedData = await FetchMaterialsAsync(tenantCode, missingCodes, cancellationToken, failGracefully: true);
        if (fetchedData is null)
        {
            return results;
        }

        foreach (var meta in fetchedData)
        {
            var cacheKey = $"material_meta:{tenantCode}:{meta.MaterialCode}";
            cache.Set(cacheKey, meta, CacheDuration);
            results[meta.MaterialCode] = meta;
        }

        return results;
    }

    public async Task<MaterialMetadata?> GetMaterialByCodeFreshAsync(string materialCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
        {
            return null;
        }

        var tenantCode = tenantContext.Current?.TenantCode;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var fetchedData = await FetchMaterialsAsync(tenantCode, [materialCode], cancellationToken, failGracefully: false);
        var fresh = fetchedData?.FirstOrDefault();
        if (fresh is null)
        {
            return null;
        }

        // Keep cache coherent with fresh reads used in critical association workflows.
        var cacheKey = $"material_meta:{tenantCode}:{fresh.MaterialCode}";
        cache.Set(cacheKey, fresh, CacheDuration);
        return fresh;
    }

    private async Task<List<MaterialMetadata>?> FetchMaterialsAsync(
        string tenantCode,
        IEnumerable<string> materialCodes,
        CancellationToken cancellationToken,
        bool failGracefully)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        var apiKey = configuration["InternalApiKey"];

        using var request = new HttpRequestMessage(HttpMethod.Post, InternalMaterialsPath);
        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Internal-Key", apiKey);
        }
        request.Content = JsonContent.Create(new { MaterialCodes = materialCodes });

        using var response = await client.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            if (failGracefully)
            {
                // Keep non-critical reads resilient for UI-oriented flows.
                return null;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Inventory metadata lookup failed. Status: {(int)response.StatusCode} {response.StatusCode}. Body: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<List<MaterialMetadata>>(cancellationToken: cancellationToken);
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

        using var request = new HttpRequestMessage(HttpMethod.Post, InternalCreateMaterialPath);
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
                // A conflict on CREATE means the SKU already belongs to an existing material.
                // Silently reusing it would allow a different product to be mapped to the wrong SKU.
                // The caller should use the "associate existing" flow instead.
                throw new RemoteServiceConflictException(
                    problem?.Code ?? "inventory.material_already_exists",
                    problem?.Detail ?? $"SKU '{input.MaterialCode}' já existe no inventário. Para utilizá-lo, use a opção 'Vincular Existente' na bancada de associação.");
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
