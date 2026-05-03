using System.ComponentModel.DataAnnotations;

namespace RailFactory.Iam.Api.Api;

public sealed record FinalizeGoogleLoginRequest(
    [property: Required, StringLength(32, MinimumLength = 2)]
    string TenantCode,
    [property: StringLength(512)]
    string? ReturnUrl,
    [property: StringLength(128)]
    string? OAuthError);
