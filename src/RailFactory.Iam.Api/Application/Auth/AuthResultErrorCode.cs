namespace RailFactory.Iam.Api.Application.Auth;

public static class AuthResultErrorCode
{
    public const string OAuthError = "oauth_error";
    public const string AuthenticationRequired = "authentication_required";
    public const string Unauthorized = "unauthorized";
    public const string TenantError = "tenant_error";
    public const string ServiceUnavailable = "service_unavailable";
}
