using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

namespace RailFactory.Logistics.Api.Infrastructure;

public sealed class ShippingAdapterFactory(
    ITenantIntegrationClient integrationClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ShippingAdapterFactory> logger) : ITenantAdapterFactory<IShippingAdapter>
{
    public async Task<IShippingAdapter> ResolveAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        using var config = await integrationClient.GetConfigAsync(tenantCode, "shipping", cancellationToken);
        if (config is null || !config.IsEnabled)
        {
            logger.LogDebug("No shipping integration for tenant {TenantCode}. Using mock.", tenantCode);
            return new MockShippingAdapter();
        }
        return ShippingAdapterBuilder.Build(config, httpClientFactory);
    }
}
