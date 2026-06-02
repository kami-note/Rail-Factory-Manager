using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application;

public sealed record RegisterTenantInput(
    string Code,
    string DisplayName,
    string Locale = "pt-BR",
    string TimeZone = "America/Sao_Paulo");

public sealed class RegisterTenant(ITenantRepository tenants)
{
    public async Task<Result<TenantDetails>> ExecuteAsync(
        RegisterTenantInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || !System.Text.RegularExpressions.Regex.IsMatch(input.Code, @"^[a-z0-9\-]{2,50}$"))
            return Result<TenantDetails>.Failure(
                Error.Validation("tenant.invalid_code", "Código deve ter 2-50 caracteres: letras minúsculas, números e hífens."));

        var existing = await tenants.FindByCodeAsync(input.Code, cancellationToken);
        if (existing is not null)
            return Result<TenantDetails>.Failure(
                Error.Conflict("tenant.already_exists", $"Tenant '{input.Code}' já existe."));

        var tenant = Tenant.Register(input.Code, input.DisplayName, input.Locale, input.TimeZone);
        await tenants.AddAsync(tenant, cancellationToken);

        return Result<TenantDetails>.Success(new TenantDetails(
            tenant.Code, tenant.DisplayName, tenant.Locale, tenant.TimeZone,
            tenant.Status.ToDisplayStatus(), tenant.ConnectionStrings));
    }
}
