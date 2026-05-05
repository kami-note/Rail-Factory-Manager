using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Tenancy;

namespace Microsoft.Extensions.Hosting;

internal sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ITenantCatalogClient tenantCatalogClient)
{
    public async Task InvokeAsync(HttpContext context, ILogger<TenantResolutionMiddleware> logger)
    {
        var path = context.Request.Path;
        var isGoogleOAuthPath = path.StartsWithSegments("/auth/google", StringComparison.OrdinalIgnoreCase);
        var isGoogleOAuthCallbackPath = path.StartsWithSegments("/auth/google/callback", StringComparison.OrdinalIgnoreCase);

        if (isGoogleOAuthCallbackPath)
        {
            await next(context);
            return;
        }

        var tenantCode = context.Request.Headers[TenantConstants.TenantCodeHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantCode) && isGoogleOAuthPath)
        {
            tenantCode = context.Request.Query["tenantCode"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            await TenantProblemResults.WriteCodeRequiredAsync(context);
            return;
        }

        var result = await tenantCatalogClient.ResolveAsync(tenantCode, context.RequestAborted);
        if (!result.Found)
        {
            await TenantProblemResults.WriteNotFoundAsync(context);
            return;
        }

        if (!result.IsActive)
        {
            await TenantProblemResults.WriteInactiveAsync(context);
            return;
        }

        var tenantContext = new TenantContext(result.Code, result.Locale, result.TimeZone, result.ConnectionStrings);
        var tenantContextAccessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
        tenantContextAccessor.Current = tenantContext;

        context.Items[TenantConstants.TenantCodeItemName] = tenantContext.TenantCode;
        context.Items[TenantConstants.TenantLocaleItemName] = tenantContext.Locale;
        context.Items[TenantConstants.TenantTimeZoneItemName] = tenantContext.TimeZone;

        Activity.Current?.SetTag("tenant.code", tenantContext.TenantCode);
        Activity.Current?.SetTag("tenant.locale", tenantContext.Locale);
        Activity.Current?.SetTag("tenant.timezone", tenantContext.TimeZone);

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["TenantCode"] = tenantContext.TenantCode,
            ["TenantLocale"] = tenantContext.Locale,
            ["TenantTimeZone"] = tenantContext.TimeZone
        }))
        {
            await next(context);
        }
    }
}
