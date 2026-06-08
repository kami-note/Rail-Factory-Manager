using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using RailFactory.BuildingBlocks.Auth;

namespace RailFactory.Frontend.Infrastructure;

public static class FrontendHostingExtensions
{
    public const string GatewayClientName = "gateway";
    private const string GatewayBaseAddress = "http://gateway";
    private const string UiDistRelativePath = "App/dist";
    private const string UiPublicRelativePath = "App/public";

    public static WebApplicationBuilder AddFrontendHosting(this WebApplicationBuilder builder)
    {
        ValidateInternalTokenOptions(builder.Configuration);
        builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
        builder.Services.Configure<InternalServiceTokenOptions>(builder.Configuration.GetSection(InternalServiceTokenOptions.SectionName));
        builder.Services.AddSingleton<PublicFrontendUrl>();
        builder.Services.AddSingleton<InternalAccessTokenIssuer>();

        builder.Services.AddHttpClient(GatewayClientName, client =>
        {
            client.BaseAddress = new Uri(GatewayBaseAddress);
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = builder.Environment.IsDevelopment() ? "railfactory-csrf" : "__Host-railfactory-csrf";
            options.Cookie.HttpOnly = false;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
                ? CookieSecurePolicy.None 
                : CookieSecurePolicy.Always;
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

        builder.Services.AddScoped<IImageStorage, MinioImageStorage>();

        return builder;
    }

    private static void ValidateInternalTokenOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(InternalServiceTokenOptions.SectionName);
        var options = section.Get<InternalServiceTokenOptions>() ?? new InternalServiceTokenOptions();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("InternalToken:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("InternalToken:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException("InternalToken:SigningKey must be configured.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("InternalToken:SigningKey must be at least 32 bytes (256 bits) for HS256.");
        }

        if (options.LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("InternalToken:LifetimeMinutes must be greater than zero.");
        }
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
