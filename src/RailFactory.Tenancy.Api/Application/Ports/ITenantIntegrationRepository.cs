using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application.Ports;

public interface ITenantIntegrationRepository
{
    Task AddAsync(TenantIntegration integration, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantIntegration integration, CancellationToken cancellationToken = default);
    Task<TenantIntegration?> FindAsync(string tenantId, string category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantIntegration>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}
