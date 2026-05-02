using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace RailFactory.Iam.Api.Infrastructure;

internal sealed class GoogleOAuthRedirects
{
    private const string GoogleFinalizePath = "/auth/google/finalize";
    private readonly GoogleOAuthOptions options;

    public GoogleOAuthRedirects(IOptions<GoogleOAuthOptions> options)
    {
        this.options = options.Value;
    }

    public string BuildFinalizeRedirectPath(string tenantCode, string returnUrl)
    {
        var query = new Dictionary<string, string?>
        {
            ["tenantCode"] = tenantCode,
            ["returnUrl"] = NormalizeFrontendReturnUrl(returnUrl)
        };

        return QueryHelpers.AddQueryString(GoogleFinalizePath, query);
    }

    public string BuildSuccessfulFrontendRedirect(string? returnUrl, string tenantCode)
    {
        var query = new Dictionary<string, string?>
        {
            ["oauth"] = "success",
            ["tenantCode"] = tenantCode
        };

        return QueryHelpers.AddQueryString(NormalizeFrontendReturnUrl(returnUrl), query);
    }

    public string BuildFailedFrontendRedirect(string? returnUrl, string tenantCode, string errorCode)
    {
        var query = new Dictionary<string, string?>
        {
            ["oauth"] = "error",
            ["tenantCode"] = tenantCode,
            ["error"] = NormalizeErrorCode(errorCode)
        };

        return QueryHelpers.AddQueryString(NormalizeFrontendReturnUrl(returnUrl), query);
    }

    private string NormalizeFrontendReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        var trimmed = returnUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Relative, out var relativeUri)
            && trimmed.StartsWith('/')
            && !trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return relativeUri.OriginalString;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri)
            && IsConfiguredPublicOrigin(absoluteUri))
        {
            return $"{absoluteUri.PathAndQuery}{absoluteUri.Fragment}";
        }

        return "/";
    }

    private bool IsConfiguredPublicOrigin(Uri uri)
    {
        var normalizedPublicOrigin = GoogleOAuthRedirectUri.NormalizePublicOrigin(options.PublicOrigin);
        if (normalizedPublicOrigin is null)
        {
            return false;
        }

        return string.Equals(uri.GetLeftPart(UriPartial.Authority), normalizedPublicOrigin, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeErrorCode(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return "oauth_error";
        }

        return errorCode.Trim().ToLowerInvariant();
    }
}
