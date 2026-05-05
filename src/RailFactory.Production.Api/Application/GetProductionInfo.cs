namespace RailFactory.Production.Api.Application;

public sealed class GetProductionInfo
{
    public ProductionInfoResponse Execute(string environment, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new ProductionInfoResponse(
            Service: "production",
            Environment: environment,
            Capability: "Manufacturing execution, work orders and quality boundary",
            Tenant: tenant);
    }
}

public sealed record ProductionInfoResponse(
    string Service,
    string Environment,
    string Capability,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Locale,
    string TimeZone);
