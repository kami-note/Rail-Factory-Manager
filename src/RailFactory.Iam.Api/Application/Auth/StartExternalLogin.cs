using RailFactory.BuildingBlocks.Auth;

namespace RailFactory.Iam.Api.Application.Auth;

public interface IExternalIdentityProvider
{
    Task<ExternalLoginStartResult> StartGoogleLoginAsync(string tenantCode, string? returnUrl, CancellationToken cancellationToken);
}

public sealed class StartExternalLogin
{
    private readonly IExternalIdentityProvider externalIdentityProvider;

    public StartExternalLogin(IExternalIdentityProvider externalIdentityProvider)
    {
        this.externalIdentityProvider = externalIdentityProvider;
    }

    public Task<ExternalLoginStartResult> ExecuteGoogleAsync(string tenantCode, string? returnUrl, CancellationToken cancellationToken)
        => externalIdentityProvider.StartGoogleLoginAsync(tenantCode, returnUrl, cancellationToken);
}

public sealed record ExternalLoginStartResult(
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    int? ErrorStatusCode,
    string? ChallengeScheme,
    string? RedirectUri)
{
    public static ExternalLoginStartResult Fail(int statusCode, string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage, statusCode, null, null);

    public static ExternalLoginStartResult Challenge(string challengeScheme, string redirectUri)
        => new(true, null, null, null, challengeScheme, redirectUri);
}
