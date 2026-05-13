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

        if (ShouldBypassTenantResolution(path))
        {
            await next(context);
            return;
        }

        var tenantCode = context.Request.Headers[TenantConstants.TenantCodeHeaderName].FirstOrDefault();
        
        // Fallback to query string for OAuth start/callback redirects where headers cannot be injected
        if (string.IsNullOrWhiteSpace(tenantCode) && IsOAuthNavigationPath(path))
        {
            tenantCode = context.Request.Query["tenantCode"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            logger.LogWarning("Tenant resolution failed: No tenant code found in header or query. Path: {Path}", path);
            await TenantProblemResults.WriteCodeRequiredAsync(context);
            return;
        }

        var result = await tenantCatalogClient.ResolveAsync(tenantCode, context.RequestAborted);
        if (!result.Found)
        {
            logger.LogWarning("Tenant resolution failed: Tenant '{TenantCode}' not found in catalog. Path: {Path}", tenantCode, path);
            await TenantProblemResults.WriteNotFoundAsync(context);
            return;
        }

        if (!result.IsActive)
        {
            logger.LogWarning("Tenant resolution failed: Tenant '{TenantCode}' is inactive. Path: {Path}", tenantCode, path);
            await TenantProblemResults.WriteInactiveAsync(context);
            return;
        }

        logger.LogDebug("Tenant resolved successfully: {TenantCode} for Path: {Path}", tenantCode, path);

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

    private static bool ShouldBypassTenantResolution(PathString path)
    {
        return path.StartsWithSegments("/auth/google/callback", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/iam/auth/google/callback", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/tenancy", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/tenants", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/status", StringComparison.OrdinalIgnoreCase) ||
               (path.Value?.EndsWith("/info", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool IsOAuthNavigationPath(PathString path)
    {
        return path.StartsWithSegments("/auth/google", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/iam/auth/google", StringComparison.OrdinalIgnoreCase);
    }
}
