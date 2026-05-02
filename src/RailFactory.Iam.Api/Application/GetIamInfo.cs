namespace RailFactory.Iam.Api.Application;

public sealed class GetIamInfo
{
    public IamInfoResponse Execute(string environment, string? tenantCode, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantCode ?? string.Empty,
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new IamInfoResponse(
            Service: "identity-access-management",
            Environment: environment,
            Capability: "Identity, access, session and authorization boundary",
            Tenant: tenant);
    }
}

public sealed record IamInfoResponse(
    string Service,
    string Environment,
    string Capability,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Code,
    string Locale,
    string TimeZone);
