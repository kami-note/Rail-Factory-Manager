namespace RailFactory.BuildingBlocks.Integrations;

public interface ITenantIntegrationClient
{
    Task<IntegrationConfig?> GetConfigAsync(string tenantCode, string category, CancellationToken cancellationToken = default);
}
