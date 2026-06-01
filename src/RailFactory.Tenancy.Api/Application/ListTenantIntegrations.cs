using RailFactory.Tenancy.Api.Application.Ports;

namespace RailFactory.Tenancy.Api.Application;

public sealed class ListTenantIntegrations(ITenantIntegrationRepository integrations)
{
    public async Task<IReadOnlyList<TenantIntegrationSummary>> ExecuteAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var list = await integrations.ListByTenantAsync(tenantId, cancellationToken);
        return list.Select(i => new TenantIntegrationSummary(
            i.Id, i.TenantId, i.Category, i.ProviderType, i.IsEnabled, i.UpdatedAt)).ToList();
    }
}

public sealed record TenantIntegrationSummary(
    Guid Id,
    string TenantId,
    string Category,
    string ProviderType,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);
