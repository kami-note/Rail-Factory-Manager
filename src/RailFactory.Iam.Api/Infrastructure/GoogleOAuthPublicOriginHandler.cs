using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace RailFactory.Iam.Api.Infrastructure;

internal sealed class GoogleOAuthPublicOriginHandler : GoogleHandler
{
    private readonly GoogleOAuthOptions googleOAuthOptions;

    public GoogleOAuthPublicOriginHandler(
        IOptionsMonitor<GoogleOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<GoogleOAuthOptions> googleOAuthOptions)
        : base(options, logger, encoder)
    {
        this.googleOAuthOptions = googleOAuthOptions.Value;
    }

    protected override Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
    {
        var publicRedirectUri = GoogleOAuthRedirectUri.BuildPublicRedirectUri(
            googleOAuthOptions.PublicOrigin,
            Options.CallbackPath);

        if (publicRedirectUri is null)
        {
            return base.ExchangeCodeAsync(context);
        }

        var publicContext = new OAuthCodeExchangeContext(
            context.Properties,
            context.Code,
            publicRedirectUri);

        return base.ExchangeCodeAsync(publicContext);
    }
}
