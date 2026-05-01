using RailFactory.BuildingBlocks.Results;

namespace RailFactory.Tenancy.Api.Application;

public sealed class GetTenantByCode
{
    private readonly ITenantRepository _tenants;

    public GetTenantByCode(ITenantRepository tenants)
    {
        _tenants = tenants;
    }

    public async Task<Result<TenantDetails>> ExecuteAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<TenantDetails>.Failure(
                Error.Validation("tenant.code_required", "Tenant code is required."));
        }

        var tenant = await _tenants.FindByCodeAsync(code, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantDetails>.Failure(
                Error.NotFound("tenant.not_found", "Tenant was not found."));
        }

        return Result<TenantDetails>.Success(new TenantDetails(
            tenant.Code,
            tenant.DisplayName,
            tenant.Locale,
            tenant.TimeZone,
            tenant.Status.ToString()));
    }
}
