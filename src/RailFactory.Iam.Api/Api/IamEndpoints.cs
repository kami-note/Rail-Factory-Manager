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
using RailFactory.Iam.Api.Api.Requests;

namespace RailFactory.Iam.Api.Api;

public static class IamEndpoints
{
    private const string ApiGroup = "/api/iam";
    private const string InfoPath = "/info";
    
    // Auth Flow
    private const string GoogleStartPath = "/auth/google/start";
    private const string GoogleFinalizePath = "/auth/google/finalize";
    
    // Session & User
    private const string SessionPath = "/auth/session";
    private const string LogoutPath = "/auth/logout";
    private const string CurrentUserPath = "/auth/current-user";
    
    // RBAC Management
    private const string RolesPath = "/roles";
    private const string PermissionsPath = "/permissions";
    private const string UsersPath = "/users";
    private const string UserRolesPath = "/users/{email}/roles";
    private const string UserSpecificRolePath = "/users/{email}/roles/{roleId:guid}";
    
    private static readonly AuthSessionDto UnauthenticatedSession = AuthSessionDto.Unauthenticated;

    public static WebApplication MapIamEndpoints(this WebApplication app)
    {
        // Root redirect for health checks/discovery
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}{InfoPath}"));

        var group = app.MapGroup(ApiGroup);

        group.MapGet(InfoPath, HandleGetInfo).AllowAnonymous();
        
        // OAuth
        group.MapGet(GoogleStartPath, HandleStartGoogleLogin).AllowAnonymous();
        group.MapGet(GoogleFinalizePath, HandleFinalizeGoogleLogin).AllowAnonymous();

        // Session
        group.MapGet(SessionPath, HandleGetSession).AllowAnonymous();
        group.MapPost(LogoutPath, (Delegate)HandleLogout).RequireAuthorization();
        group.MapGet(CurrentUserPath, HandleGetCurrentUser).RequireAuthorization();

        // RBAC Management
        var adminGroup = group.MapGroup("/").RequirePermission(SystemPermissions.Iam.RolesManage);
        
        adminGroup.MapGet(RolesPath, HandleListRoles);
        adminGroup.MapPost(RolesPath, HandleCreateRole);
        adminGroup.MapGet(PermissionsPath, () => Results.Ok(SystemPermissions.All()));
        adminGroup.MapGet(UsersPath, HandleListUsers);
        adminGroup.MapPost(UserRolesPath, HandleAssignRole);
        adminGroup.MapDelete(UserSpecificRolePath, HandleRemoveRole);

        return app;
    }

    private static async Task<IResult> HandleListRoles(
        ListTenantRoles listTenantRoles,
        CancellationToken cancellationToken)
    {
        var roles = await listTenantRoles.ExecuteAsync(cancellationToken);
        return Results.Ok(roles);
    }

    private static async Task<IResult> HandleCreateRole(
        CreateRoleRequest request,
        CreateTenantRole createTenantRole,
        CancellationToken cancellationToken)
    {
        var id = await createTenantRole.ExecuteAsync(request, cancellationToken);
        return Results.Created($"{ApiGroup}{RolesPath}/{id}", new { id });
    }

    private static async Task<IResult> HandleAssignRole(
        string email,
        AssignRoleRequest request,
        AssignRoleToUser assignRoleToUser,
        CancellationToken cancellationToken)
    {
        var success = await assignRoleToUser.ExecuteAsync(email, request.RoleId, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleListUsers(
        ListTenantUsers listTenantUsers,
        CancellationToken cancellationToken)
    {
        var users = await listTenantUsers.ExecuteAsync(cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> HandleRemoveRole(
        string email,
        Guid roleId,
        RemoveRoleFromUser removeRoleFromUser,
        CancellationToken cancellationToken)
    {
        var success = await removeRoleFromUser.ExecuteAsync(email, roleId, cancellationToken);
        return success ? Results.NoContent() : Results.NotFound();
    }

    private static IResult HandleDebugGoogle(
        [FromServices] Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions> googleOptions,
        [FromServices] Microsoft.Extensions.Options.IOptions<RailFactory.Iam.Api.Infrastructure.GoogleOAuthOptions> customOptions)
    {
        var opt = googleOptions.Get(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        return Results.Ok(new
        {
            CustomPublicOrigin = customOptions.Value.PublicOrigin,
            CustomCallbackPath = customOptions.Value.CallbackPath,
            AuthEndpoint = opt.AuthorizationEndpoint,
            ClientId = opt.ClientId
        });
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
            email: context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email"),
            displayName: context.User.Identity?.Name,
            cancellationToken);

        return upsertResult.Success
            ? Results.Redirect(redirects.BuildSuccessfulFrontendRedirect(result.ReturnUrl, result.TenantCode))
            : Results.Redirect(redirects.BuildFailedFrontendRedirect(
                result.ReturnUrl,
                result.TenantCode,
                upsertResult.ErrorCode ?? AuthResultErrorCode.OAuthError));
    }

    private static async Task<IResult> HandleGetSession(
        HttpContext context,
        [FromServices] GetUserPermissions getUserPermissions,
        CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated is not true)
        {
            return Results.Json(UnauthenticatedSession, statusCode: StatusCodes.Status401Unauthorized);
        }

        var provider = "google";
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        
        var permissions = await getUserPermissions.ExecuteAsync(provider, subject!, cancellationToken);

        return Results.Ok(AuthSessionDto.CreateAuthenticated(
            context.User.Identity?.Name,
            context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email"),
            permissions));
    }

    private static async Task<IResult> HandleLogout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleGetCurrentUser(
        HttpContext context,
        [FromServices] GetUserPermissions getUserPermissions,
        CancellationToken cancellationToken)
    {
        var provider = "google";
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        
        var permissions = await getUserPermissions.ExecuteAsync(provider, subject!, cancellationToken);

        return Results.Ok(AuthSessionDto.CreateAuthenticated(
            context.User.Identity?.Name,
            context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email"),
            permissions));
    }
}
