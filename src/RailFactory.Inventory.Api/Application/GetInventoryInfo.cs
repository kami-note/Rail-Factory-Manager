namespace RailFactory.Inventory.Api.Application;

public sealed class GetInventoryInfo
{
    public InventoryInfoResponse Execute(string environment, string? tenantCode, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantCode ?? string.Empty,
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new InventoryInfoResponse(
            Service: "inventory",
            Environment: environment,
            Capability: "Stock balance, reservation and ledger boundary",
            Tenant: tenant);
    }
}

public sealed record InventoryInfoResponse(
    string Service,
    string Environment,
    string Capability,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Code,
    string Locale,
    string TimeZone);
