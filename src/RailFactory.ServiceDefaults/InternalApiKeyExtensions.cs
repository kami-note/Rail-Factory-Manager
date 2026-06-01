using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

public static class InternalApiKeyExtensions
{
    /// <summary>
    /// Adds an endpoint filter that requires a valid X-Internal-Key header matching InternalApiKey config.
    /// </summary>
    public static TBuilder RequireInternalApiKey<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(static (ctx, next) =>
        {
            var configuration = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expected = configuration["InternalApiKey"];

            if (string.IsNullOrWhiteSpace(expected))
                return ValueTask.FromResult<object?>(Results.Problem(
                    title: "Internal API key is not configured.",
                    statusCode: StatusCodes.Status500InternalServerError));

            // Hash both sides before comparing so FixedTimeEquals always runs to completion
            // regardless of input length, preventing a timing oracle on key length.
            if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Internal-Key", out var provided)
                || !CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString())),
                    SHA256.HashData(Encoding.UTF8.GetBytes(expected))))
                return ValueTask.FromResult<object?>(Results.Unauthorized());

            return next(ctx);
        });
    }
}
