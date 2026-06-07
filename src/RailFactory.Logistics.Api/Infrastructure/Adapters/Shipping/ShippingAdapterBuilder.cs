using System.Net.Http.Headers;
using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

internal static class ShippingAdapterBuilder
{
    internal static IShippingAdapter Build(IntegrationConfig config, IHttpClientFactory httpClientFactory) =>
        config.ProviderType switch
        {
            "melhorenvio" => BuildMelhorEnvio(config, httpClientFactory),
            _             => new MockShippingAdapter()
        };

    private static MelhorEnvioAdapter BuildMelhorEnvio(IntegrationConfig config, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("melhorenvio");

        if (config.Credentials.TryGetString("base_url", out var baseUrl) && !string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl);

        if (!config.Credentials.TryGetString("access_token", out var token) || string.IsNullOrEmpty(token))
            throw new InvalidOperationException(
                "Melhor Envio integration is missing the required 'access_token' credential.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (config.Credentials.TryGetString("client_id", out var clientId) && !string.IsNullOrEmpty(clientId))
            client.DefaultRequestHeaders.Add("User-Agent", $"RailFactory/{clientId}");

        return new MelhorEnvioAdapter(client);
    }
}
