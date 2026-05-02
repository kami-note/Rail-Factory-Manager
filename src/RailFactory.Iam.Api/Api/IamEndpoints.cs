using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using RailFactory.Iam.Api.Application;
using RailFactory.Iam.Api.Infrastructure;

namespace RailFactory.Iam.Api.Api;

public static class IamEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";
    private const string GoogleStartPath = "/auth/google/start";
    private const string SessionPath = "/auth/session";
    private const string GoogleFinalizePath = "/auth/google/finalize";
    private static readonly AuthSessionResponse UnauthenticatedSession = AuthSessionResponse.Unauthenticated;

    public static WebApplication MapIamEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapGet(GoogleStartPath, HandleStartGoogleLogin);
        app.MapGet(SessionPath, HandleGetSession);
        app.MapGet(GoogleFinalizePath, HandleFinalizeGoogleLogin);
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetIamInfo getIamInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getIamInfo.Execute(
            environment.EnvironmentName,
            tenant?.Code,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleStartGoogleLogin(
        string tenantCode,
        string? returnUrl,
        [FromServices] GoogleOAuthRedirects redirects,
        ITenantCatalogClient tenantCatalogClient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var resolvedTenant = await tenantCatalogClient.ResolveAsync(tenantCode.Trim(), cancellationToken);
        if (!resolvedTenant.Found)
        {
            return Results.NotFound(new { code = TenantConstants.NotFoundErrorCode });
        }

        if (!resolvedTenant.IsActive)
        {
            return Results.BadRequest(new { code = TenantConstants.InactiveErrorCode });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirects.BuildFinalizeRedirectPath(resolvedTenant.Code, returnUrl ?? "/")
        };

        return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
    }

    private static IResult HandleFinalizeGoogleLogin(
        HttpContext context,
        string tenantCode,
        string? returnUrl,
        string? oauthError,
        [FromServices] GoogleOAuthRedirects redirects)
    {
        if (!string.IsNullOrWhiteSpace(oauthError))
        {
            return Results.Redirect(redirects.BuildFailedFrontendRedirect(returnUrl, tenantCode, oauthError));
        }

        if (context.User.Identity?.IsAuthenticated is not true)
        {
            return Results.Redirect(redirects.BuildFailedFrontendRedirect(returnUrl, tenantCode, "authentication_required"));
        }

        return Results.Redirect(redirects.BuildSuccessfulFrontendRedirect(returnUrl, tenantCode));
    }

    private static IResult HandleGetSession(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated is not true)
        {
            return Results.Json(UnauthenticatedSession, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(AuthSessionResponse.CreateAuthenticated(
            context.User.Identity?.Name,
            context.User.FindFirst("email")?.Value));
    }

    private sealed record AuthSessionResponse(bool Authenticated, AuthSessionUserResponse? User)
    {
        public static AuthSessionResponse Unauthenticated { get; } = new(false, null);

        public static AuthSessionResponse CreateAuthenticated(string? name, string? email)
            => new(true, new AuthSessionUserResponse(name, email));
    }

    private sealed record AuthSessionUserResponse(string? Name, string? Email);
}
