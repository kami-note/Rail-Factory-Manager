namespace RailFactory.SupplyChain.Api.Application;

public sealed class GetSupplyChainInfo
{
    public SupplyChainInfoResponse Execute(string environment, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new SupplyChainInfoResponse(
            Service: "supply-chain",
            Environment: environment,
            Tenant: tenant);
    }
}

public sealed record SupplyChainInfoResponse(
    string Service,
    string Environment,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Locale,
    string TimeZone);
