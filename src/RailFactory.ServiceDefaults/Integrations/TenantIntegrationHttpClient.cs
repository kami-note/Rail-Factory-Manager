using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using RailFactory.BuildingBlocks.Integrations;

namespace Microsoft.Extensions.Hosting;

public sealed class TenantIntegrationHttpClient(
    HttpClient httpClient,
    IConfiguration configuration) : ITenantIntegrationClient
{
    public async Task<IntegrationConfig?> GetConfigAsync(
        string tenantCode, string category, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["InternalApiKey"]
            ?? throw new InvalidOperationException("InternalApiKey is not configured.");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/tenancy/integrations/{Uri.EscapeDataString(tenantCode)}/{Uri.EscapeDataString(category)}/credentials");
        request.Headers.Add("X-Internal-Key", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<IntegrationCredentialsResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Integration credentials endpoint returned empty response.");

        return new IntegrationConfig(
            dto.ProviderType,
            SecureCredentials.FromStrings(dto.Credentials),
            isEnabled: true);
    }

    private sealed record IntegrationCredentialsResponse(
        string ProviderType,
        IReadOnlyDictionary<string, string> Credentials);
}
