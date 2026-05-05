using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Iam.Api.Application;
using RailFactory.Iam.Api.Application.Auth;
using RailFactory.Iam.Api.Infrastructure;
using RailFactory.Iam.Api.Api.Validation;

namespace RailFactory.Iam.Api.Api;

public static class IamEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";
    private const string GoogleStartPath = "/auth/google/start";
    private const string SessionPath = "/auth/session";
    private const string LogoutPath = "/auth/logout";
    private const string CurrentUserPath = "/auth/current-user";
    private const string GoogleFinalizePath = "/auth/google/finalize";
    private static readonly AuthSessionDto UnauthenticatedSession = AuthSessionDto.Unauthenticated;

    public static WebApplication MapIamEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo).AllowAnonymous();
        app.MapGet(GoogleStartPath, HandleStartGoogleLogin).AllowAnonymous();
        app.MapGet(SessionPath, HandleGetSession).AllowAnonymous();
        app.MapPost(LogoutPath, (Func<HttpContext, Task<IResult>>)HandleLogout).RequireAuthorization();
        app.MapGet(CurrentUserPath, HandleGetCurrentUser).RequireAuthorization();
        app.MapGet(GoogleFinalizePath, HandleFinalizeGoogleLogin).AllowAnonymous();
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetIamInfo getIamInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getIamInfo.Execute(
            environment.EnvironmentName,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleStartGoogleLogin(
        [AsParameters] StartGoogleLoginRequest request,
        StartExternalLogin startExternalLogin,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var result = await startExternalLogin.ExecuteGoogleAsync(request.TenantCode, request.ReturnUrl, cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorStatusCode == StatusCodes.Status400BadRequest && result.ErrorCode == TenantConstants.CodeRequiredErrorCode)
            {
                return TenantHttpResults.CodeRequired();
            }

            return Results.Json(
                new AuthErrorDto(result.ErrorCode ?? AuthResultErrorCode.TenantError, result.ErrorMessage ?? "Authentication start failed."),
                statusCode: result.ErrorStatusCode ?? StatusCodes.Status400BadRequest);
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = result.RedirectUri
        };

        return Results.Challenge(properties, [result.ChallengeScheme ?? GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> HandleFinalizeGoogleLogin(
        HttpContext context,
        [AsParameters] FinalizeGoogleLoginRequest request,
        [FromServices] GoogleOAuthRedirects redirects,
        [FromServices] FinalizeExternalLogin finalizeExternalLogin,
        [FromServices] UpsertLocalUserFromExternalLogin upsertLocalUserFromExternalLogin,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var result = finalizeExternalLogin.Execute(
            context.User.Identity?.IsAuthenticated is true,
            request.TenantCode,
            request.ReturnUrl,
            request.OAuthError);

        if (!result.Success)
        {
            return Results.Redirect(redirects.BuildFailedFrontendRedirect(
                result.ReturnUrl,
                result.TenantCode,
                result.ErrorCode ?? AuthResultErrorCode.OAuthError));
        }

        var upsertResult = await upsertLocalUserFromExternalLogin.ExecuteAsync(
            externalProvider: "google",
            externalSubject: context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub"),
            email: context.User.FindFirstValue("email"),
            displayName: context.User.Identity?.Name,
            cancellationToken);

        return upsertResult.Success
            ? Results.Redirect(redirects.BuildSuccessfulFrontendRedirect(result.ReturnUrl, result.TenantCode))
            : Results.Redirect(redirects.BuildFailedFrontendRedirect(
                result.ReturnUrl,
                result.TenantCode,
                upsertResult.ErrorCode ?? AuthResultErrorCode.OAuthError));
    }

    private static IResult HandleGetSession(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated is not true)
        {
            return Results.Json(UnauthenticatedSession, statusCode: StatusCodes.Status401Unauthorized);
        }

        return Results.Ok(AuthSessionDto.CreateAuthenticated(
            context.User.Identity?.Name,
            context.User.FindFirst("email")?.Value));
    }

    private static async Task<IResult> HandleLogout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static IResult HandleGetCurrentUser(HttpContext context)
        => Results.Ok(AuthSessionDto.CreateAuthenticated(
            context.User.Identity?.Name,
            context.User.FindFirst("email")?.Value));
}
