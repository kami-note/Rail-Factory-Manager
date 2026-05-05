using RailFactory.BuildingBlocks.Persistence;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application;

public interface ITenantRepository : IRepository<Tenant, string>
{
    Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default);
}
