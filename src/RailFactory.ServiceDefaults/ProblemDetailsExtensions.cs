using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

internal static class ProblemDetailsExtensions
{
    public static TBuilder AddRailFactoryProblemDetails<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var httpContext = context.HttpContext;
                var correlationId = CorrelationIdAccessor.GetOrCreate(httpContext);

                context.ProblemDetails.Extensions["correlationId"] = correlationId;
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["service"] = builder.Environment.ApplicationName;
            };
        });

        return builder;
    }

    public static WebApplication UseRailFactoryExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var correlationId = CorrelationIdAccessor.GetOrCreate(context);
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RailFactory.UnhandledException");

                if (exception is not null)
                {
                    logger.LogError(exception, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers[ServiceDefaultsKeys.CorrelationIdHeaderName] = correlationId;

                var problem = new ProblemDetails
                {
                    Type = "https://httpstatuses.com/500",
                    Title = "Unexpected server error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
                    Instance = context.Request.Path
                };

                problem.Extensions["code"] = "unexpected_error";
                problem.Extensions["correlationId"] = correlationId;
                problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
                problem.Extensions["service"] = app.Environment.ApplicationName;

                await context.Response.WriteAsJsonAsync(problem);
            });
        });

        return app;
    }
}
