namespace RailFactory.Iam.Api.Application.Auth;

public interface IIamLocalUserRepository
{
    Task UpsertAsync(IamLocalUser user, CancellationToken cancellationToken);
}

public sealed record IamLocalUser(
    string TenantCode,
    string ExternalProvider,
    string ExternalSubject,
    string? Email,
    string? DisplayName);

public sealed class UpsertLocalUserFromExternalLogin(IIamLocalUserRepository repository)
{
    public async Task<UpsertLocalUserResult> ExecuteAsync(
        string tenantCode,
        string externalProvider,
        string? externalSubject,
        string? email,
        string? displayName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return UpsertLocalUserResult.Failed(AuthResultErrorCode.TenantError);
        }

        if (string.IsNullOrWhiteSpace(externalProvider) || string.IsNullOrWhiteSpace(externalSubject))
        {
            return UpsertLocalUserResult.Failed(AuthResultErrorCode.OAuthError);
        }

        var user = new IamLocalUser(
            tenantCode.Trim(),
            externalProvider.Trim().ToLowerInvariant(),
            externalSubject.Trim(),
            Normalize(email),
            Normalize(displayName));

        await repository.UpsertAsync(user, cancellationToken);
        return UpsertLocalUserResult.Succeeded();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record UpsertLocalUserResult(bool Success, string? ErrorCode)
{
    public static UpsertLocalUserResult Succeeded() => new(true, null);
    public static UpsertLocalUserResult Failed(string errorCode) => new(false, errorCode);
}
