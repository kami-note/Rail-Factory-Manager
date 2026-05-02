using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

public static class FrontendEndpoints
{
    private const string RootPath = "/";
    private const string StatusPath = "/api/status";
    private const string AuthGoogleStartPath = "/api/auth/google/start";
    private const string AuthSessionPath = "/api/auth/session";
    private const string AuthCsrfPath = "/api/auth/csrf";
    private const string AuthLogoutPath = "/api/auth/logout";

    public static WebApplication MapFrontendEndpoints(this WebApplication app, FrontendHostingExtensions.FrontendStaticUiState staticUi)
    {
        app.MapGet(RootPath, () => Results.Redirect(StatusPath));
        app.MapGet(AuthGoogleStartPath, GoogleLoginEndpoint.HandleStart);
        app.MapGet(AuthSessionPath, FrontendAuthSessionEndpoint.HandleGet);
        app.MapGet(AuthCsrfPath, FrontendCsrfEndpoint.HandleGet);
        app.MapPost(AuthLogoutPath, FrontendLogoutEndpoint.HandlePost);
        app.MapGet(StatusPath, FrontendStatusEndpoint.HandleGet);
        app.MapReverseProxy();

        if (staticUi.Enabled)
        {
            app.MapFallback(httpContext => SpaFallbackEndpoint.Handle(httpContext, staticUi.DistDirectory));
        }

        return app;
    }
}
