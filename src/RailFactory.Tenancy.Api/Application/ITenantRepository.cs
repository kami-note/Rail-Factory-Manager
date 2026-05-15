using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application;

public interface ITenantRepository
{
    Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default);
}
