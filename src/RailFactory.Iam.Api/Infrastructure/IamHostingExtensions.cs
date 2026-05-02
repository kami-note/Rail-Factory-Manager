using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;

namespace RailFactory.Iam.Api.Infrastructure;

public static class IamHostingExtensions
{
    public static WebApplicationBuilder AddIamHosting(this WebApplicationBuilder builder)
    {
        var googleOAuth = builder.Configuration
            .GetSection(GoogleOAuthOptions.SectionName)
            .Get<GoogleOAuthOptions>() ?? new GoogleOAuthOptions();
        ValidateGoogleOAuthOptions(googleOAuth);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection(GoogleOAuthOptions.SectionName));
        builder.Services.AddSingleton<GoogleOAuthRedirects>();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddOAuth<GoogleOptions, GoogleOAuthPublicOriginHandler>(GoogleDefaults.AuthenticationScheme, options =>
            {
                ConfigureGoogleDefaults(options);
                options.ClientId = googleOAuth.ClientId;
                options.ClientSecret = googleOAuth.ClientSecret;
                GoogleOAuthRedirectUri.ApplyPublicOrigin(options, googleOAuth);
                options.Events = new OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("RailFactory.Iam.Api.OAuth");

                        var failureMessage = context.Failure?.Message ?? "oauth_remote_failure";
                        logger.LogWarning(
                            "OAuth remote failure on callback. ErrorCode={ErrorCode}; Path={Path}",
                            NormalizeOAuthErrorCode(failureMessage),
                            context.Request.Path.Value);

                        var redirectTarget = context.Properties?.RedirectUri;
                        if (string.IsNullOrWhiteSpace(redirectTarget))
                        {
                            redirectTarget = "/auth/google/finalize";
                        }

                        var safeRedirect = QueryHelpers.AddQueryString(
                            redirectTarget!,
                            "oauthError",
                            NormalizeOAuthErrorCode(failureMessage));

                        context.Response.Redirect(safeRedirect);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

        return builder;
    }

    public static WebApplication UseIamHosting(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseServiceDefaults();
        app.UseAuthentication();
        app.UseTenantResolution();
        app.MapDefaultEndpoints();
        return app;
    }

    private static void ValidateGoogleOAuthOptions(GoogleOAuthOptions options)
    {
        if (!IsGoogleOAuthActive(options))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("Authentication:Google:ClientId must be configured when Google OAuth is active.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("Authentication:Google:ClientSecret must be configured when Google OAuth is active.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicOrigin))
        {
            throw new InvalidOperationException("Authentication:Google:PublicOrigin must be configured when Google OAuth is active.");
        }

        GoogleOAuthRedirectUri.NormalizePublicOrigin(options.PublicOrigin);
        GoogleOAuthRedirectUri.NormalizeCallbackPath(options.CallbackPath);
    }

    private static bool IsGoogleOAuthActive(GoogleOAuthOptions options)
        => !string.IsNullOrWhiteSpace(options.ClientId)
            || !string.IsNullOrWhiteSpace(options.ClientSecret)
            || !string.IsNullOrWhiteSpace(options.PublicOrigin)
            || !string.Equals(options.CallbackPath, GoogleOAuthOptions.DefaultCallbackPath, StringComparison.Ordinal);

    private static void ConfigureGoogleDefaults(GoogleOptions options)
    {
        options.AuthorizationEndpoint = GoogleDefaults.AuthorizationEndpoint;
        options.TokenEndpoint = GoogleDefaults.TokenEndpoint;
        options.UserInformationEndpoint = GoogleDefaults.UserInformationEndpoint;

        if (!options.Scope.Contains("openid", StringComparer.Ordinal))
        {
            options.Scope.Add("openid");
        }

        if (!options.Scope.Contains("profile", StringComparer.Ordinal))
        {
            options.Scope.Add("profile");
        }

        if (!options.Scope.Contains("email", StringComparer.Ordinal))
        {
            options.Scope.Add("email");
        }
    }

    private static string NormalizeOAuthErrorCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return "oauth_error";
        }

        return errorCode
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "_", StringComparison.Ordinal);
    }
}
