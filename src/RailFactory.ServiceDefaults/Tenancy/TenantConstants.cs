namespace Microsoft.Extensions.Hosting;

public static class TenantConstants
{
    public const string TenantCodeHeaderName = "X-Tenant-Code";
    public const string UserEmailHeaderName = "X-RF-User-Email";
    public const string UserNameHeaderName = "X-RF-User-Name";
    public const string UserPermissionsHeaderName = "X-RF-User-Permissions";
    public const string TenantCodeItemName = "TenantCode";
    public const string TenantLocaleItemName = "TenantLocale";
    public const string TenantTimeZoneItemName = "TenantTimeZone";

    public const string CodeRequiredErrorCode = "tenant.code_required";
    public const string NotFoundErrorCode = "tenant.not_found";
    public const string InactiveErrorCode = "tenant.inactive";
}
