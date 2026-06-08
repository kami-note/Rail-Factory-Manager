using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

/// <summary>
/// Endpoint for serving person profile images from tenant storage.
/// Requires 'hr.read' permission.
/// </summary>
internal static class PersonImageServingEndpoint
{
    private const string IamSessionPath = "/api/iam/auth/session";

    public static async Task<IResult> HandleGet(
        [FromRoute] string tenantCode,
        [FromRoute] string fileName,
        HttpContext httpContext,
        IImageStorage storage,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // 1. Validate Tenant Code (Sanitization)
        if (string.IsNullOrWhiteSpace(tenantCode) || !System.Text.RegularExpressions.Regex.IsMatch(tenantCode, "^[A-Za-z0-9_-]+$"))
        {
            return Results.NotFound();
        }

        // 2. Validate Authentication & RBAC
        var session = await GetAuthenticatedSessionAsync(httpContext, tenantCode, httpClientFactory, cancellationToken);
        if (session == null)
        {
            return Results.Unauthorized();
        }

        if (session.User == null)
        {
            return Results.Unauthorized();
        }

        if (!session.User.Permissions.Contains(SystemPermissions.Hr.Read, StringComparer.Ordinal))
        {
            return Results.Forbid();
        }

        // 3. Serve File via Abstraction
        var imageResult = await storage.GetAsync(tenantCode, fileName, cancellationToken);
        
        if (imageResult == null)
        {
            return Results.NotFound();
        }

        return Results.File(imageResult.Stream, imageResult.ContentType);
    }

    private static async Task<AuthSessionDto?> GetAuthenticatedSessionAsync(
        HttpContext httpContext,
        string tenantCode,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, IamSessionPath);
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return payload?.Authenticated == true ? payload : null;
    }
}
