using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Application.Ports;

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
}
