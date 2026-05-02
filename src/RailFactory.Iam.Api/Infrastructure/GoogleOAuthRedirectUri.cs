using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace RailFactory.Iam.Api.Infrastructure;

internal static class GoogleOAuthRedirectUri
{
    public static void ApplyPublicOrigin(GoogleOptions options, GoogleOAuthOptions googleOAuth)
    {
        options.CallbackPath = NormalizeCallbackPath(googleOAuth.CallbackPath);

        var redirectUri = BuildPublicRedirectUri(googleOAuth.PublicOrigin, options.CallbackPath);
        if (redirectUri is null)
        {
            return;
        }

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var authorizationEndpoint = ReplaceQueryStringValue(
                context.RedirectUri,
                "redirect_uri",
                redirectUri);

            context.Response.Redirect(authorizationEndpoint);
            return Task.CompletedTask;
        };
    }

    public static string? BuildPublicRedirectUri(string? publicOrigin, PathString callbackPath)
    {
        var normalizedPublicOrigin = NormalizePublicOrigin(publicOrigin);
        return normalizedPublicOrigin is null ? null : $"{normalizedPublicOrigin}{callbackPath}";
    }

    public static PathString NormalizeCallbackPath(string? callbackPath)
    {
        if (string.IsNullOrWhiteSpace(callbackPath))
        {
            return GoogleOAuthOptions.DefaultCallbackPath;
        }

        var trimmed = callbackPath.Trim();
        if (trimmed.Contains('?') || trimmed.Contains('#'))
        {
            throw new InvalidOperationException("Authentication:Google:CallbackPath must not contain query string or fragment.");
        }

        return trimmed.StartsWith('/') ? trimmed : $"/{trimmed}";
    }

    public static string? NormalizePublicOrigin(string? publicOrigin)
    {
        if (string.IsNullOrWhiteSpace(publicOrigin))
        {
            return null;
        }

        var trimmed = publicOrigin.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Authentication:Google:PublicOrigin must be an absolute HTTPS origin.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException("Authentication:Google:PublicOrigin must be an absolute HTTPS origin without path, query, or fragment.");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static string ReplaceQueryStringValue(string uri, string key, string value)
    {
        var builder = new UriBuilder(uri);
        var parsedQuery = QueryHelpers.ParseQuery(builder.Query);
        var query = new Dictionary<string, StringValues>(parsedQuery, StringComparer.Ordinal)
        {
            [key] = value
        };

        builder.Query = QueryString.Create(query).ToUriComponent().TrimStart('?');
        return builder.Uri.ToString();
    }
}
