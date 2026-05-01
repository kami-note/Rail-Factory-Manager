using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly Dictionary<string, Tenant> _tenants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dev"] = Tenant.RegisterDevTenant()
    };

    public Task<Tenant?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync(id, cancellationToken);
    }

    public Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        _tenants.TryGetValue(code, out var tenant);
        return Task.FromResult(tenant);
    }

    public Task AddAsync(Tenant aggregate, CancellationToken cancellationToken = default)
    {
        _tenants[aggregate.Code] = aggregate;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
