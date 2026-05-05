namespace RailFactory.Iam.Api.Application;

public sealed class GetIamInfo
{
    public IamInfoResponse Execute(string environment, string? tenantLocale, string? tenantTimeZone)
    {
        var tenant = new TenantInfoResponse(
            tenantLocale ?? string.Empty,
            tenantTimeZone ?? string.Empty);

        return new IamInfoResponse(
            Service: "identity-access-management",
            Environment: environment,
            Capability: "Authentication, identity federation and authorization boundary",
            Tenant: tenant);
    }
}

public sealed record IamInfoResponse(
    string Service,
    string Environment,
    string Capability,
    TenantInfoResponse Tenant);

public sealed record TenantInfoResponse(
    string Locale,
    string TimeZone);
