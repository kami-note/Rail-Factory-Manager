using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

namespace RailFactory.Logistics.Api.Infrastructure;

public sealed class FiscalIssuerAdapterFactory(
    ITenantIntegrationClient integrationClient,
    IHttpClientFactory httpClientFactory,
    ILogger<FiscalIssuerAdapterFactory> logger) : ITenantAdapterFactory<IFiscalIssuerAdapter>
{
    public async Task<IFiscalIssuerAdapter> ResolveAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        using var config = await integrationClient.GetConfigAsync(tenantCode, "fiscal", cancellationToken);

        if (config is null || !config.IsEnabled)
        {
            logger.LogDebug("No fiscal integration configured for tenant {TenantCode}. Using mock adapter.", tenantCode);
            return new MockFiscalIssuerAdapter();
        }

        return FiscalAdapterBuilder.Build(config, httpClientFactory);
    }
}
