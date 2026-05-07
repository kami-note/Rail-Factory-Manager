using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;

namespace RailFactory.Frontend.Infrastructure;

public static class FrontendHostingExtensions
{
    public const string GatewayClientName = "gateway";
    private const string GatewayBaseAddress = "http://gateway";
    private const string UiDistRelativePath = "App/dist";
    private const string UiPublicRelativePath = "App/public";

    public static WebApplicationBuilder AddFrontendHosting(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
        builder.Services.AddSingleton<PublicFrontendUrl>();

        builder.Services.AddHttpClient(GatewayClientName, client =>
        {
            client.BaseAddress = new Uri(GatewayBaseAddress);
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-railfactory-csrf";
            options.Cookie.HttpOnly = false;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();

        builder.Services.AddScoped<IImageStorage, LocalImageStorage>();

        return builder;
    }

    public static WebApplication UseFrontendHosting(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseServiceDefaults();
        app.MapDefaultEndpoints();
        return app;
    }

    public static FrontendStaticUiState UseFrontendStaticUi(this WebApplication app)
    {
        var uiDistDirectory = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, UiDistRelativePath));
        var uiPublicDirectory = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, UiPublicRelativePath));
        var hasUiDist = Directory.Exists(uiDistDirectory);
        var hasUiPublic = Directory.Exists(uiPublicDirectory);

        if (hasUiPublic)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uiPublicDirectory),
                RequestPath = ""
            });
        }

        if (!hasUiDist)
        {
            return FrontendStaticUiState.Disabled;
        }

        var fileProvider = new PhysicalFileProvider(uiDistDirectory);
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider
        });

        return new FrontendStaticUiState(true, uiDistDirectory);
    }

    public readonly record struct FrontendStaticUiState(bool Enabled, string DistDirectory)
    {
        public static FrontendStaticUiState Disabled => new(false, string.Empty);
    }
}
