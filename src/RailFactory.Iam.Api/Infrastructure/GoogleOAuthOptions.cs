namespace RailFactory.Iam.Api.Infrastructure;

public sealed class GoogleOAuthOptions
{
    public const string SectionName = "Authentication:Google";
    public const string DefaultCallbackPath = "/auth/google/callback";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string PublicOrigin { get; init; } = string.Empty;

    public string CallbackPath { get; init; } = DefaultCallbackPath;
}
