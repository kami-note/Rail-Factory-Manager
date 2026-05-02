namespace RailFactory.Iam.Api.Application.Auth;

public sealed class FinalizeExternalLogin
{
    public ExternalLoginFinalizeResult Execute(bool isAuthenticated, string tenantCode, string? returnUrl, string? oauthError)
    {
        if (!string.IsNullOrWhiteSpace(oauthError))
        {
            return ExternalLoginFinalizeResult.Failed(tenantCode, returnUrl, NormalizeOAuthErrorCode(oauthError));
        }

        if (!isAuthenticated)
        {
            return ExternalLoginFinalizeResult.Failed(tenantCode, returnUrl, AuthResultErrorCode.AuthenticationRequired);
        }

        return ExternalLoginFinalizeResult.Succeeded(tenantCode, returnUrl);
    }

    private static string NormalizeOAuthErrorCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return AuthResultErrorCode.OAuthError;
        }

        return errorCode.Trim().ToLowerInvariant().Replace(" ", "_", StringComparison.Ordinal);
    }
}

public sealed record ExternalLoginFinalizeResult(bool Success, string TenantCode, string ReturnUrl, string? ErrorCode)
{
    public static ExternalLoginFinalizeResult Succeeded(string tenantCode, string? returnUrl)
        => new(true, tenantCode, returnUrl ?? "/", null);

    public static ExternalLoginFinalizeResult Failed(string tenantCode, string? returnUrl, string errorCode)
        => new(false, tenantCode, returnUrl ?? "/", errorCode);
}
