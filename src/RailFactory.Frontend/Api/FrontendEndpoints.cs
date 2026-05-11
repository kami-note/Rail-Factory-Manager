using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;
using System.Net.Http.Json;

namespace RailFactory.Frontend.Api;

public static class FrontendEndpoints
{
    private const string RootPath = "/";
    private const string StatusPath = "/api/status";
    private const string AuthGoogleStartPath = "/api/auth/google/start";
    private const string AuthSessionPath = "/api/auth/session";
    private const string AuthCsrfPath = "/api/auth/csrf";
    private const string AuthLogoutPath = "/api/auth/logout";
    private const string MaterialImageUploadPath = "/api/materials/{materialCode}/image";
    private const string MaterialImageServingPath = "/api/inventory/materials/images/{tenantCode}/{fileName}";

    public static WebApplication MapFrontendEndpoints(this WebApplication app, FrontendHostingExtensions.FrontendStaticUiState staticUi)
    {
        app.MapGet(RootPath, () => Results.Redirect(StatusPath));
        app.MapGet(AuthGoogleStartPath, GoogleLoginEndpoint.HandleStart);
        app.MapGet(AuthSessionPath, FrontendAuthSessionEndpoint.HandleGet);
        app.MapGet(AuthCsrfPath, FrontendCsrfEndpoint.HandleGet);
        app.MapPost(AuthLogoutPath, FrontendLogoutEndpoint.HandlePost);
        app.MapPost(MaterialImageUploadPath, MaterialImageUploadEndpoint.HandlePost);
        app.MapGet(MaterialImageServingPath, MaterialImageServingEndpoint.HandleGet);
        app.MapGet(StatusPath, FrontendStatusEndpoint.HandleGet);
        
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
                    if (context.Request.Headers.TryGetValue("Cookie", out var cookies))
                    {
                        request.Headers.Add("Cookie", cookies.ToString());
                    }

                    try
                    {
                        using var response = await gateway.SendAsync(request, context.RequestAborted);
                        if (response.IsSuccessStatusCode)
                        {
                            var session = await response.Content.ReadFromJsonAsync<AuthSessionDto>(context.RequestAborted);
                            if (session?.Authenticated == true && session.User != null)
                            {
                                // Inject trusted headers for internal microservices
                                context.Request.Headers[TenantConstants.UserEmailHeaderName] = session.User.Email;
                                context.Request.Headers[TenantConstants.UserNameHeaderName] = session.User.Name;
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
}
