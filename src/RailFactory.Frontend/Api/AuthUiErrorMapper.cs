using RailFactory.BuildingBlocks.Auth;

namespace RailFactory.Frontend.Api;

public static class AuthUiErrorMapper
{
    public static AuthErrorDto MapFromStatusCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => new AuthErrorDto("unauthorized", "Authentication is required."),
            StatusCodes.Status403Forbidden => new AuthErrorDto("csrf_error", "CSRF validation failed."),
            StatusCodes.Status400BadRequest or StatusCodes.Status404NotFound => new AuthErrorDto("tenant_error", "Tenant is invalid or missing."),
            _ => new AuthErrorDto("oauth_error", "Authentication flow failed.")
        };
    }
}
