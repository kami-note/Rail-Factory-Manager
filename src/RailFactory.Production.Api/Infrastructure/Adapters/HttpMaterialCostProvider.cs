using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Infrastructure.Adapters;

/// <summary>
/// HTTP implementation of the <see cref="IMaterialCostProvider"/> port.
/// Queries the Supply Chain boundary internally to fetch the latest material unit costs.
/// </summary>
public sealed class HttpMaterialCostProvider(
    IHttpClientFactory httpClientFactory,
    ITenantContextAccessor tenantContext,
    IConfiguration configuration) : IMaterialCostProvider
{
    private const string ClientName = "supply-chain-integration";
    private const string Path = "/api/supply-chain/internal/material-costs";

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, decimal>> GetMaterialCostsAsync(
        IEnumerable<string> materialCodes,
        CancellationToken cancellationToken = default)
    {
        if (materialCodes == null || !materialCodes.Any())
        {
            return new Dictionary<string, decimal>();
        }

        var tenantCode = tenantContext.Current?.TenantCode;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            throw new InvalidOperationException("Tenant context is missing.");
        }

        var client = httpClientFactory.CreateClient(ClientName);
        var apiKey = configuration["InternalApiKey"];

        // Format query params as a series of code parameters.
        var queryString = string.Join("&", materialCodes.Select(c => $"codes={Uri.EscapeDataString(c)}"));
        var requestUri = $"{Path}?{queryString}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("X-Internal-Key", apiKey);
        }

        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Supply chain material cost lookup failed. Status: {(int)response.StatusCode} {response.StatusCode}. Body: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, decimal>>(cancellationToken: cancellationToken);
        return result ?? new Dictionary<string, decimal>();
    }
}
