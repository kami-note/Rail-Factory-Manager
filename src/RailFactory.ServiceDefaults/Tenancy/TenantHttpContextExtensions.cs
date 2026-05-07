using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class TenantHttpContextExtensions
{
    public static TenantInfoDto? GetResolvedTenant(this HttpContext context)
    {
        if (!context.Items.TryGetValue(TenantConstants.TenantCodeItemName, out var code)
            || code is not string tenantCode
            || string.IsNullOrWhiteSpace(tenantCode))
        {
            return null;
        }

        var locale = context.Items[TenantConstants.TenantLocaleItemName] as string ?? string.Empty;
        var timeZone = context.Items[TenantConstants.TenantTimeZoneItemName] as string ?? string.Empty;

        var connectionStrings = context.RequestServices.GetService<ITenantContextAccessor>()?.Current?.ConnectionStrings
                               ?? new Dictionary<string, string>();

        return new TenantInfoDto(tenantCode, locale, timeZone, connectionStrings);
    }

    public static string? ReadTenantCodeHeader(this HttpContext context)
        => context.Request.Headers[TenantConstants.TenantCodeHeaderName].FirstOrDefault();
}
