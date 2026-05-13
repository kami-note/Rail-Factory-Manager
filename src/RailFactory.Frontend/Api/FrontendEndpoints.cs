using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;
using System.Net.Http.Json;

namespace RailFactory.Frontend.Api;

public static class FrontendEndpoints
{
    private const string RootPath = "/";
    private const string StatusPath = "/api/frontend/status";
    private const string AuthGoogleStartPath = "/api/iam/auth/google/start";
    private const string AuthSessionPath = "/api/iam/auth/session";
    private const string AuthCsrfPath = "/api/iam/auth/csrf";
    private const string AuthLogoutPath = "/api/iam/auth/logout";
    private const string MaterialImageUploadPath = "/api/inventory/materials/{materialCode}/image";
    private const string MaterialImageServingPath = "/api/inventory/materials/images/{tenantCode}/{fileName}";

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
        
        app.MapReverseProxy(proxyPipeline =>
        {
            // ELITE FIX: Add Edge Security and Identity Forwarding for proxied calls
            proxyPipeline.Use(async (context, next) =>
            {
                // 1. HTTPS Normalization (critical for CSRF and Secure Cookies)
                if (!context.Request.IsHttps && string.Equals(context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase))
                {
                    context.Request.Scheme = "https";
                }

                // 2. CSRF Validation for mutation methods (POST, PUT, DELETE)
                if (HttpMethods.IsPost(context.Request.Method) || 
                    HttpMethods.IsPut(context.Request.Method) || 
                    HttpMethods.IsDelete(context.Request.Method))
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

                // 3. Identity Propagation (Token Exchange: Cookie -> Trusted Headers)
                // We validate the session against IAM and forward the user info to downstream services.
                var tenantCode = context.ReadTenantCodeHeader();
                if (!string.IsNullOrWhiteSpace(tenantCode))
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
                                var resolvedUserEmail = FirstNonEmpty(session.User.Email, session.User.Name);
                                var resolvedUserName = FirstNonEmpty(session.User.Name, session.User.Email);

                                // Inject trusted headers for internal microservices
                                if (!string.IsNullOrWhiteSpace(resolvedUserEmail))
                                {
                                    context.Request.Headers[TenantConstants.UserEmailHeaderName] = resolvedUserEmail;
                                }

                                if (!string.IsNullOrWhiteSpace(resolvedUserName))
                                {
                                    context.Request.Headers[TenantConstants.UserNameHeaderName] = resolvedUserName;
                                }

                                if (session.User.Permissions.Any())
                                {
                                    context.Request.Headers[TenantConstants.UserPermissionsHeaderName] = string.Join(",", session.User.Permissions);
                                }
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
}
