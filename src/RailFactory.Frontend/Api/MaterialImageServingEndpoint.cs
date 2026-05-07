using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class MaterialImageServingEndpoint
{
    public static async Task<IResult> HandleGet(
        [FromRoute] string tenantCode,
        [FromRoute] string fileName,
        HttpContext httpContext,
        IImageStorage storage,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // 1. Validate Tenant Code (Same sanitization as upload)
        if (string.IsNullOrWhiteSpace(tenantCode) || !System.Text.RegularExpressions.Regex.IsMatch(tenantCode, "^[A-Za-z0-9_-]+$"))
        {
            return Results.NotFound();
        }

        // 2. Validate Authentication & Tenant Access
        if (!await IsAuthenticatedAsync(httpContext, tenantCode, httpClientFactory, cancellationToken))
        {
            return Results.Unauthorized();
        }

        // 3. Serve File via Abstraction
        var imageResult = await storage.GetAsync(tenantCode, fileName, cancellationToken);
        
        if (imageResult == null)
        {
            return Results.NotFound();
        }

        return Results.File(imageResult.Stream, imageResult.ContentType);
    }

    private static async Task<bool> IsAuthenticatedAsync(
        HttpContext httpContext,
        string tenantCode,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // Reusing the session check logic. In a real production system, this could be optimized with local session caching.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/iam/auth/session");
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode) return false;

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return payload?.Authenticated == true;
    }
}
