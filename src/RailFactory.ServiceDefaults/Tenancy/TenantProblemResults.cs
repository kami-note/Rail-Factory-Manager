using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

internal static class TenantProblemResults
{
    public static Task WriteCodeRequiredAsync(HttpContext context)
        => WriteAsync(context, TenantConstants.CodeRequiredErrorCode, "Tenant code is required.", StatusCodes.Status400BadRequest);

    public static Task WriteNotFoundAsync(HttpContext context)
        => WriteAsync(context, TenantConstants.NotFoundErrorCode, "Tenant was not found.", StatusCodes.Status404NotFound);

    public static Task WriteInactiveAsync(HttpContext context)
        => WriteAsync(context, TenantConstants.InactiveErrorCode, "Tenant is inactive.", StatusCodes.Status403Forbidden);

    private static Task WriteAsync(HttpContext context, string code, string detail, int statusCode)
    {
        var result = Results.Problem(
            title: statusCode == StatusCodes.Status404NotFound ? "Resource not found" : "Invalid request",
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code
            });

        return result.ExecuteAsync(context);
    }
}
