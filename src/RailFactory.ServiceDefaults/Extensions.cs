using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

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
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.ShouldHandle = static args =>
                {
                    var statusCode = args.Outcome.Result?.StatusCode;
                    if (statusCode is null)
                    {
                        return ValueTask.FromResult(false);
                    }

                    var code = (int)statusCode.Value;
                    return ValueTask.FromResult(code is 408 or 429 || code >= 500);
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
}
