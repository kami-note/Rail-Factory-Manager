using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RailFactory.BuildingBlocks.Auth;

namespace RailFactory.Iam.Api.Infrastructure;

public static class IamHostingExtensions
{
    private const string SmartAuthScheme = "SmartAuth";

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
            // ForwardLimit=4 handles the full proxy chain:
            // Browser → Ngrok → Vite → BFF → Gateway → IAM
            // Without this, only the rightmost hop (gateway) is processed,
            // causing IAM to see an internal host instead of the ngrok public URL.
            options.ForwardLimit = 4;
        });

        builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection(GoogleOAuthOptions.SectionName));
        builder.AddRedisDistributedCache("redis");

        // Persist Data Protection keys to Redis so IAM restarts do not invalidate
        // existing session cookies. Redis is volume-backed in the Aspire setup.
        builder.Services.AddDataProtection()
            .SetApplicationName("railfactory-iam");
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
            new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new DistributedCacheXmlRepository(
                    sp.GetRequiredService<IDistributedCache>());
            }));
        builder.Services.AddSingleton<GoogleOAuthRedirects>();
        builder.Services.AddAuthorization();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = SmartAuthScheme;
                options.DefaultAuthenticateScheme = SmartAuthScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(SmartAuthScheme, "Selects cookie or internal bearer auth.", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorization = context.Request.Headers.Authorization.FirstOrDefault();
                    return !string.IsNullOrWhiteSpace(authorization)
                        && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? InternalServiceTokenAuthenticationExtensions.Scheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })
            .AddInternalTokenAuthentication(builder.Configuration)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
                    ? CookieSecurePolicy.None 
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = builder.Environment.IsDevelopment()
                    ? SameSiteMode.Unspecified
                    : SameSiteMode.Lax;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        if (IsApiRequest(context.Request))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return context.Response.WriteAsJsonAsync(AuthSessionDto.Unauthenticated);
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        if (IsApiRequest(context.Request))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return context.Response.WriteAsJsonAsync(new
                            {
                                code = "forbidden",
                                message = "You do not have permission to access this resource."
                            });
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            })
            .AddOAuth<GoogleOptions, GoogleOAuthPublicOriginHandler>(GoogleDefaults.AuthenticationScheme, options =>
            {
                ConfigureGoogleDefaults(options);
                options.ClientId = googleOAuth.ClientId;
                options.ClientSecret = googleOAuth.ClientSecret;
                GoogleOAuthRedirectUri.ApplyPublicOrigin(options, googleOAuth);
                var existingEvents = options.Events;
                options.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = existingEvents.OnRedirectToAuthorizationEndpoint,
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
                            redirectTarget = "/api/iam/auth/google/finalize";
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
        app.UseInternalTokenTenantBinding();
        app.UseAuthorization();
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

    private static bool IsApiRequest(HttpRequest request)
        => request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
}
