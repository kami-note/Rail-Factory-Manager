using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

internal static class PaymentGatewayAdapterBuilder
{
    internal static IPaymentGatewayAdapter Build(IntegrationConfig config, IHttpClientFactory httpClientFactory) =>
        config.ProviderType switch
        {
            "asaas" => BuildAsaas(config, httpClientFactory),
            _       => new MockPaymentGatewayAdapter()
        };

    private static AsaasAdapter BuildAsaas(IntegrationConfig config, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("asaas");

        if (config.Credentials.TryGetString("base_url", out var baseUrl) && !string.IsNullOrEmpty(baseUrl))
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        if (!config.Credentials.TryGetString("access_token", out var token) || string.IsNullOrEmpty(token))
            throw new InvalidOperationException(
                "Asaas integration is missing the required 'access_token' credential.");

        // Asaas authenticates via access_token header and requires User-Agent
        client.DefaultRequestHeaders.Add("access_token", token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "RailFactory/1.0");

        return new AsaasAdapter(client);
    }
}
