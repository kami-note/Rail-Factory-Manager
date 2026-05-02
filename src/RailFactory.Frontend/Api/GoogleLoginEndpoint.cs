using Microsoft.AspNetCore.Mvc;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class GoogleLoginEndpoint
{
    private const string IamGoogleStartPath = "/api/iam/auth/google/start";

    public static IResult HandleStart(
        HttpContext context,
        [FromServices] PublicFrontendUrl publicFrontendUrl,
        string? tenantCode,
        string? returnUrl)
    {
        var resolvedTenantCode = ResolveTenantCode(context, tenantCode);
        if (string.IsNullOrWhiteSpace(resolvedTenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["tenantCode"] = resolvedTenantCode.Trim(),
            ["returnUrl"] = publicFrontendUrl.BuildPublicReturnUrl(returnUrl)
        });

        return Results.Redirect($"{IamGoogleStartPath}{query}");
    }

    private static string? ResolveTenantCode(HttpContext context, string? tenantCode)
    {
        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            return tenantCode;
        }

        return context.Request.Query["tenantCode"].FirstOrDefault();
    }
}
