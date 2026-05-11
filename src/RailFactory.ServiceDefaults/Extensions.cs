using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Net.Http;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.AddRailFactoryProblemDetails();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // ELITE FIX: Optimize connection lifetime for containerized environments (DNS changes)
            http.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            });

            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.ShouldHandle = static args =>
                {
                    var statusCode = args.Outcome.Result?.StatusCode;
                    if (statusCode is null)
                    {
                        return ValueTask.FromResult(true); // Network failure, safe to retry
                    }

                    var code = (int)statusCode.Value;
                    
                    // Always safe to retry 429 (Too Many Requests) or 408 (Request Timeout).
                    // We avoid retrying 500 automatically on non-idempotent verbs here
                    // to prevent duplicate processing without Idempotency-Keys.
                    return ValueTask.FromResult(code is 429 or 408);
                };
            });
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder AddTenantResolution<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddRailFactoryTenantResolution();
        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseRailFactoryExceptionHandler();
        app.UseRailFactoryCorrelationScope();
        return app;
    }

    public static WebApplication UseTenantResolution(this WebApplication app)
    {
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }

    public static WebApplication UseRailFactoryHeaderIdentity(this WebApplication app)
    {
        app.UseMiddleware<HeaderIdentityMiddleware>();
        return app;
    }
}
