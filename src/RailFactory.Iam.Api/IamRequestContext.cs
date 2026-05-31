using Microsoft.AspNetCore.Http;

namespace RailFactory.Iam.Api;

internal static class IamRequestContext
{
    internal static string? ExtractIpAddress(HttpContext? context)
    {
        if (context is null) return null;
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return context.Connection.RemoteIpAddress?.ToString();
    }

    internal static string? ExtractCorrelationId(HttpContext? context) =>
        context?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? context?.TraceIdentifier;
}
