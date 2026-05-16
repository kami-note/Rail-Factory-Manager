using Microsoft.AspNetCore.Antiforgery;

namespace RailFactory.Frontend.Api;

internal static class FrontendCsrfEndpoint
{
    public static IResult HandleGet(HttpContext httpContext, IAntiforgery antiforgery, IHostEnvironment environment)
    {
        // When running behind a public HTTPS tunnel/proxy, ensure scheme reflects forwarded proto.
        if (!httpContext.Request.IsHttps
            && string.Equals(httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Request.Scheme = "https";
        }

        if (!httpContext.Request.IsHttps && !environment.IsDevelopment())
        {
            return Results.Json(
                new { code = "csrf_https_required", message = "CSRF token endpoint requires HTTPS." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return Results.Ok(new { token = tokens.RequestToken });
    }
}
