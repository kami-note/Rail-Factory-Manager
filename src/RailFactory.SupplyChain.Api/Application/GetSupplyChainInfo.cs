namespace RailFactory.SupplyChain.Api.Application;

public sealed class GetSupplyChainInfo
{
    public SupplyChainInfoResponse Execute(string environment, string? tenantCode, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantCode ?? string.Empty,
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new SupplyChainInfoResponse(
            Service: "supply-chain",
            Environment: environment,
            Capability: "Receiving, supplier collaboration and inbound material boundary",
            Tenant: tenant);
    }
}

public sealed record SupplyChainInfoResponse(
    string Service,
    string Environment,
    string Capability,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Code,
    string Locale,
    string TimeZone);
