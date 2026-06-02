using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application.Ports;

namespace RailFactory.Tenancy.Api.Application;

public sealed class EnableIntegration(ITenantIntegrationRepository integrations)
{
    public async Task<Result<TenantIntegrationSummary>> ExecuteAsync(
        string tenantId, string category, CancellationToken cancellationToken = default)
    {
        var integration = await integrations.FindAsync(tenantId, category, cancellationToken);
        if (integration is null)
            return Result<TenantIntegrationSummary>.Failure(
                Error.NotFound("integration.not_found", "Integration not found."));

        integration.Enable();
        await integrations.UpdateAsync(integration, cancellationToken);
        return Result<TenantIntegrationSummary>.Success(
            new TenantIntegrationSummary(integration.Id, integration.TenantId, integration.Category,
                integration.ProviderType, integration.IsEnabled, integration.UpdatedAt));
    }
}

public sealed class DisableIntegration(ITenantIntegrationRepository integrations)
{
    public async Task<Result<TenantIntegrationSummary>> ExecuteAsync(
        string tenantId, string category, CancellationToken cancellationToken = default)
    {
        var integration = await integrations.FindAsync(tenantId, category, cancellationToken);
        if (integration is null)
            return Result<TenantIntegrationSummary>.Failure(
                Error.NotFound("integration.not_found", "Integration not found."));

        integration.Disable();
        await integrations.UpdateAsync(integration, cancellationToken);
        return Result<TenantIntegrationSummary>.Success(
            new TenantIntegrationSummary(integration.Id, integration.TenantId, integration.Category,
                integration.ProviderType, integration.IsEnabled, integration.UpdatedAt));
    }
}
