using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;
using System.Net.Http.Json;

namespace RailFactory.Frontend.Api;

public static class FrontendEndpoints
{
    private const string RootPath = "/";
    private const string StatusPath = "/api/frontend/status";

    public static WebApplication MapFrontendEndpoints(this WebApplication app, FrontendHostingExtensions.FrontendStaticUiState staticUi)
    {
        // Root redirect
        app.MapGet(RootPath, () => Results.Redirect(StatusPath));

        var group = app.MapGroup("/api");

        group.MapGet("/frontend/status", FrontendStatusEndpoint.HandleGet);
        group.MapGet("/status", FrontendStatusEndpoint.HandleGet); // Alias for frontend usage
        
        // IAM Group
        var iamGroup = group.MapGroup("/iam");
        iamGroup.MapGet("/auth/google/start", GoogleLoginEndpoint.HandleStart);
        iamGroup.MapGet("/auth/session", FrontendAuthSessionEndpoint.HandleGet);
        iamGroup.MapGet("/auth/csrf", FrontendCsrfEndpoint.HandleGet);
        iamGroup.MapPost("/auth/logout", FrontendLogoutEndpoint.HandlePost);

        // Inventory Group
        var inventoryGroup = group.MapGroup("/inventory");
        inventoryGroup.MapPut("/materials/{materialCode}/image", MaterialImageUploadEndpoint.HandlePut);
        inventoryGroup.MapGet("/materials/images/{tenantCode}/{fileName}", MaterialImageServingEndpoint.HandleGet);

        // HR Group
        var hrGroup = group.MapGroup("/hr");
        hrGroup.MapPut("/people/{id:guid}/image", PersonImageUploadEndpoint.HandlePut);
        hrGroup.MapGet("/people/images/{tenantCode}/{fileName}", PersonImageServingEndpoint.HandleGet);
        
        app.MapReverseProxy(proxyPipeline =>
        {
            // ELITE FIX: Add Edge Security and Identity Forwarding for proxied calls
            proxyPipeline.Use(async (context, next) =>
            {
                // DEV BYPASS: detect X-Dev-User before stripping headers
                var devEmail = app.Environment.IsDevelopment()
                    && context.Request.Headers.TryGetValue("X-Dev-User", out var devHeader)
                    && !string.IsNullOrWhiteSpace(devHeader)
                    ? devHeader.ToString()
                    : null;

                StripSensitiveProxyHeaders(context); // also strips X-Dev-User from downstream

                // 1. HTTPS Normalization (critical for CSRF and Secure Cookies)
                if (!context.Request.IsHttps && string.Equals(context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Scheme = "https";
                }

                // 2. CSRF Validation for mutation methods (POST, PUT, DELETE) — skipped for dev bypass
                // Bootstrap has no tenant yet and is protected by its own "zero tenants" gate.
                var isBootstrap = context.Request.Path.StartsWithSegments("/api/tenancy/bootstrap");
                if (!isBootstrap && devEmail is null && (HttpMethods.IsPost(context.Request.Method) ||
                    HttpMethods.IsPut(context.Request.Method) ||
                    HttpMethods.IsDelete(context.Request.Method)))
                {
                    var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                    try
                    {
                        await antiforgery.ValidateRequestAsync(context);
                    }
                    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { code = "csrf_error", message = "CSRF token validation failed." });
                        return;
                    }
                }

                // 3. Identity Propagation (Validated Session -> Internal Trusted Headers)
                var tenantCode = context.ReadTenantCodeHeader();
                if (!string.IsNullOrWhiteSpace(tenantCode))
                {
                    if (devEmail is not null)
                    {
                        // DEV BYPASS: issue internal token directly, skip IAM call
                        var fakeSession = AuthSessionDto.CreateAuthenticated(devEmail, devEmail, SystemPermissions.All());
                        var tokenIssuer = context.RequestServices.GetRequiredService<InternalAccessTokenIssuer>();
                        var internalToken = tokenIssuer.Issue(fakeSession, tenantCode);
                        context.Request.Headers.Authorization = $"Bearer {internalToken}";
                    }
                    else
                    {
                        var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);

                        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/iam/auth/session");
                        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);

                        // Propagate authentication cookie
                        if (context.Request.Headers.TryGetValue("Cookie", out var cookies))
                        {
                            request.Headers.Add("Cookie", cookies.ToString());
                        }

                        // Propagate forwarding headers to ensure IAM respects Secure/SameSite cookie policies
                        if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
                        {
                            request.Headers.Add("X-Forwarded-Proto", proto.ToString());
                        }
                        else if (context.Request.IsHttps)
                        {
                            request.Headers.Add("X-Forwarded-Proto", "https");
                        }

                        if (context.Request.Headers.TryGetValue("X-Forwarded-Host", out var host))
                        {
                            request.Headers.Add("X-Forwarded-Host", host.ToString());
                        }

                        try
                        {
                            using var response = await gateway.SendAsync(request, context.RequestAborted);
                            if (response.IsSuccessStatusCode)
                            {
                                var session = await response.Content.ReadFromJsonAsync<AuthSessionDto>(context.RequestAborted);
                                if (session?.Authenticated == true && session.User != null)
                                {
                                    var tokenIssuer = context.RequestServices.GetRequiredService<InternalAccessTokenIssuer>();
                                    var internalToken = tokenIssuer.Issue(session, tenantCode);
                                    context.Request.Headers.Authorization = $"Bearer {internalToken}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log and continue without identity (downstream will handle unauthorized if needed)
                            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("BffIdentityForwarder");
                            logger.LogWarning(ex, "Failed to forward identity for proxied request.");
                        }
                    }
                }

                await next();
            });
        });

        if (staticUi.Enabled)
        {
            app.MapFallback(httpContext => SpaFallbackEndpoint.Handle(httpContext, staticUi.DistDirectory));
        }

        return app;
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        return string.IsNullOrWhiteSpace(second) ? null : second;
    }

    private static void StripSensitiveProxyHeaders(HttpContext context)
    {
        context.Request.Headers.Remove("Authorization");
        context.Request.Headers.Remove(TenantConstants.UserEmailHeaderName);
        context.Request.Headers.Remove(TenantConstants.UserNameHeaderName);
        context.Request.Headers.Remove(TenantConstants.UserPermissionsHeaderName);
        context.Request.Headers.Remove("X-Internal-Key");
        context.Request.Headers.Remove("X-Dev-User"); // prevent dev bypass leaking to downstream
    }
}
