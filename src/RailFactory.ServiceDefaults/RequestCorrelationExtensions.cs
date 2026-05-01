using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

internal static class RequestCorrelationExtensions
{
    public static WebApplication UseRailFactoryCorrelationScope(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = CorrelationIdAccessor.GetOrCreate(context);
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Remove(ServiceDefaultsKeys.CorrelationIdHeaderName);
                context.Response.Headers[ServiceDefaultsKeys.CorrelationIdHeaderName] = correlationId;
                return Task.CompletedTask;
            });

            Activity.Current?.SetTag("correlation.id", correlationId);

            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("RailFactory.Request");

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            }))
            {
                await next(context);
            }
        });

        return app;
    }
}
