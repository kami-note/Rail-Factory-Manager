namespace RailFactory.Frontend.Api;

internal static class SpaFallbackEndpoint
{
    private const string ApiPathPrefix = "/api";
    private const string AuthGooglePathPrefix = "/auth/google";

    public static async Task Handle(HttpContext httpContext, string uiDistDirectory)
    {
        if (httpContext.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase)
            || httpContext.Request.Path.StartsWithSegments(AuthGooglePathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var indexPath = Path.Combine(uiDistDirectory, "index.html");
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.SendFileAsync(indexPath);
    }
}
