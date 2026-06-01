using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Mfa;

/// <summary>
/// Disables MFA for a user, requiring a valid TOTP code as confirmation (RF-07).
/// </summary>
public sealed class DisableMfa(IamAuthDbContext dbContext)
{
    public async Task ExecuteAsync(string provider, string subject, string totpCode, CancellationToken ct)
    {
        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.ExternalProvider == provider && u.ExternalSubject == subject, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!user.MfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecretBase32))
            throw new InvalidOperationException("MFA is not enabled.");

        if (!TotpService.Verify(user.MfaSecretBase32, totpCode))
            throw new UnauthorizedAccessException("Invalid TOTP code.");

        user.MfaEnabled = false;
        user.MfaSecretBase32 = null;
        user.MfaEnabledAt = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
    }
}
