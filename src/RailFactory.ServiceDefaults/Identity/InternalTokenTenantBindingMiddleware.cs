using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Auth;

namespace Microsoft.Extensions.Hosting;

internal sealed class InternalTokenTenantBindingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<InternalTokenTenantBindingMiddleware> logger, IHostEnvironment environment)
    {
        if (context.User.Identity?.IsAuthenticated != true || environment.IsDevelopment())
        {
            await next(context);
            return;
        }

        var tokenTenant = context.User.FindFirst(InternalServiceTokenClaimTypes.Tenant)?.Value;
        if (string.IsNullOrWhiteSpace(tokenTenant))
        {
            await next(context);
            return;
        }

        var resolvedTenant = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(resolvedTenant))
        {
            logger.LogWarning(
                "Authenticated internal token request has no resolved tenant. Path={Path}",
                context.Request.Path);

            await WriteTenantMismatchAsync(context);
            return;
        }

        if (!string.Equals(tokenTenant, resolvedTenant, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Internal token tenant mismatch. TokenTenant={TokenTenant}; ResolvedTenant={ResolvedTenant}; Path={Path}",
                tokenTenant,
                resolvedTenant,
                context.Request.Path);

            await WriteTenantMismatchAsync(context);
            return;
        }

        await next(context);
    }

    private static Task WriteTenantMismatchAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new
        {
            code = "tenant.mismatch",
            message = "The authenticated internal token does not match the resolved tenant."
        });
    }
}
