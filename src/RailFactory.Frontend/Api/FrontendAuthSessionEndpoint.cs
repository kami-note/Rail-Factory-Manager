using System.Net;
using System.Net.Http.Json;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class FrontendAuthSessionEndpoint
{
    private const string IamSessionPath = "/api/iam/auth/session";
    private static readonly AuthSessionDto UnauthenticatedSession = AuthSessionDto.Unauthenticated;

    public static async Task<IResult> HandleGet(
        HttpContext httpContext,
        IHostEnvironment env,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var tenantCode = httpContext.ReadTenantCodeHeader();
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        // DEV BYPASS: X-Dev-User header → skip Google OAuth entirely
        if (env.IsDevelopment()
            && httpContext.Request.Headers.TryGetValue("X-Dev-User", out var devEmailHeader)
            && !string.IsNullOrWhiteSpace(devEmailHeader))
        {
            var devEmail = devEmailHeader.ToString();
            var fakeSession = AuthSessionDto.CreateAuthenticated(devEmail, devEmail, SystemPermissions.All());
            return Results.Ok(fakeSession);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, IamSessionPath);
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);

        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        // ELITE FIX FOR NGROK: Forward all proxy headers to ensure IAM recognizes the public host/proto
        foreach (var header in httpContext.Request.Headers.Where(h => h.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase)))
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
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

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return Results.Ok(payload ?? UnauthenticatedSession);
    }
}
