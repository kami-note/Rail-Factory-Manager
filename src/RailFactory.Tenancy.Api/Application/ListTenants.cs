using RailFactory.BuildingBlocks.Results;

namespace RailFactory.Tenancy.Api.Application;

public sealed class ListTenants(ITenantRepository tenants)
{
    public async Task<Result<IReadOnlyList<TenantDetails>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allTenants = await tenants.ListTenantsAsync(cancellationToken);
        
        var details = allTenants.Select(tenant => new TenantDetails(
            tenant.Code,
            tenant.DisplayName,
            tenant.Locale,
            tenant.TimeZone,
            tenant.Status.ToString(),
            tenant.ConnectionStrings)).ToList();

        return Result<IReadOnlyList<TenantDetails>>.Success(details);
    }
}
