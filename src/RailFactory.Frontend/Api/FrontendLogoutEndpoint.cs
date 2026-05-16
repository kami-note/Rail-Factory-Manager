using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class FrontendLogoutEndpoint
{
    private const string IamLogoutPath = "/api/iam/auth/logout";
    private static readonly AuthSessionDto UnauthenticatedSession = AuthSessionDto.Unauthenticated;

    public static async Task<IResult> HandlePost(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // When running behind a public HTTPS tunnel/proxy, honor forwarded scheme before CSRF validation.
        if (!httpContext.Request.IsHttps
            && string.Equals(httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Request.Scheme = "https";
        }

        if (!httpContext.Request.IsHttps)
        {
            return Results.Json(
                new AuthErrorDto("csrf_https_required", "CSRF token validation requires HTTPS."),
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Json(new AuthErrorDto("csrf_error", "CSRF token validation failed."), statusCode: StatusCodes.Status403Forbidden);
        }

        var tenantCode = httpContext.ReadTenantCodeHeader();
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, IamLogoutPath);
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);

        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var unauthorizedPayload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
            return Results.Json(unauthorizedPayload ?? UnauthenticatedSession, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.Json(
                AuthUiErrorMapper.MapFromStatusCode((int)response.StatusCode),
                statusCode: (int)response.StatusCode);
        }

        // Forward the Set-Cookie header from IAM so the browser actually clears
        // the session cookie. Without this, logout has no effect on the browser.
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var cookie in setCookies)
            {
                httpContext.Response.Headers.Append("Set-Cookie", cookie);
            }
        }

        return Results.NoContent();
    }
}
