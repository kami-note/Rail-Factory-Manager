using System.Net;
using System.Net.Http.Json;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class FrontendAuthSessionEndpoint
{
    private const string IamSessionPath = "/api/iam/auth/session";
    private static readonly AuthSessionDto UnauthenticatedSession = AuthSessionDto.Unauthenticated;

    public static async Task<IResult> HandleGet(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var tenantCode = httpContext.ReadTenantCodeHeader();
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, IamSessionPath);
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
            return Results.StatusCode((int)response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return Results.Ok(payload ?? UnauthenticatedSession);
    }

    private sealed record AuthSessionDto(bool Authenticated, SessionUserDto? User)
    {
        public static AuthSessionDto Unauthenticated { get; } = new(false, null);
    }

    private sealed record SessionUserDto(string? Name, string? Email);
}
