using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

/// <summary>
/// Builds fiscal provider adapters from a resolved IntegrationConfig.
/// Shared between FiscalIssuerAdapterFactory (per-request) and LogisticsFiscalDispatcher (background).
/// </summary>
internal static class FiscalAdapterBuilder
{
    internal static IFiscalIssuerAdapter Build(IntegrationConfig config, IHttpClientFactory httpClientFactory) =>
        config.ProviderType switch
        {
            "plugnotas" => BuildPlugNotas(config, httpClientFactory),
            "focusnfe"  => BuildFocusNfe(config, httpClientFactory),
            _           => new MockFiscalIssuerAdapter()
        };

    private static PlugNotasAdapter BuildPlugNotas(IntegrationConfig config, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("plugnotas");
        if (config.Credentials.TryGetString("base_url", out var baseUrl) && !string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);

        if (!config.Credentials.TryGetString("api_key", out var apiKey) || string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException(
                "PlugNotas integration is missing the required 'api_key' credential.");

        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        return new PlugNotasAdapter(client);
    }

    private static FocusNfeAdapter BuildFocusNfe(IntegrationConfig config, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("focusnfe");
        if (config.Credentials.TryGetString("base_url", out var baseUrl) && !string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);

        if (!config.Credentials.TryGetString("token", out var token) || string.IsNullOrEmpty(token))
            throw new InvalidOperationException(
                "FocusNFe integration is missing the required 'token' credential.");

        var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{token}:"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
        return new FocusNfeAdapter(client);
    }
}
