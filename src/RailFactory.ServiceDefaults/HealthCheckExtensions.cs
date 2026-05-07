using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

public static class HealthCheckExtensions
{
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // ELITE FIX: Background service health check
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // Note: Specific infrastructure checks (DB, Redis) should be added in the individual service modules
        // to avoid polluting ServiceDefaults with project-specific connection strings.

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(ServiceDefaultsKeys.HealthEndpointPath);
            app.MapHealthChecks(ServiceDefaultsKeys.AliveEndpointPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            });
        }

        return app;
    }
}
