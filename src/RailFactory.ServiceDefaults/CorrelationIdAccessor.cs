using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

internal static class CorrelationIdAccessor
{
    public static string GetOrCreate(HttpContext context)
    {
        if (context.Items.TryGetValue(ServiceDefaultsKeys.CorrelationIdHeaderName, out var existing)
            && existing is string existingCorrelationId
            && !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            return existingCorrelationId;
        }

        var headerValue = context.Request.Headers[ServiceDefaultsKeys.CorrelationIdHeaderName].FirstOrDefault();
        var correlationId = !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue
            : Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.Items[ServiceDefaultsKeys.CorrelationIdHeaderName] = correlationId;
        return correlationId;
    }
}
