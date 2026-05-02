using Microsoft.Extensions.Options;

namespace RailFactory.Frontend.Infrastructure;

public sealed class PublicFrontendUrl
{
    private readonly string publicOrigin;

    public PublicFrontendUrl(IOptions<FrontendOptions> options)
    {
        publicOrigin = NormalizePublicOrigin(options.Value.PublicOrigin)
            ?? throw new InvalidOperationException("Frontend:PublicOrigin must be configured as an absolute HTTPS origin before OAuth login can start.");
    }

    public string BuildPublicReturnUrl(string? returnUrl)
        => $"{publicOrigin}{NormalizeReturnPath(returnUrl)}";

    private static string? NormalizePublicOrigin(string? publicOrigin)
    {
        if (string.IsNullOrWhiteSpace(publicOrigin))
        {
            return null;
        }

        var trimmed = publicOrigin.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException("Frontend:PublicOrigin must be an absolute HTTPS origin without path, query, or fragment.");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private string NormalizeReturnPath(string? returnUrl)
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
            && string.Equals(absoluteUri.GetLeftPart(UriPartial.Authority), publicOrigin, StringComparison.OrdinalIgnoreCase))
        {
            return $"{absoluteUri.PathAndQuery}{absoluteUri.Fragment}";
        }

        return "/";
    }
}
