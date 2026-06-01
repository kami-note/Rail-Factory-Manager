using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Mfa;

/// <summary>
/// Activates MFA for a user after they prove ownership of the authenticator app (RF-07).
/// </summary>
public sealed class ConfirmMfa(IamAuthDbContext dbContext)
{
    public async Task ExecuteAsync(string provider, string subject, string totpCode, CancellationToken ct)
    {
        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.ExternalProvider == provider && u.ExternalSubject == subject, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrWhiteSpace(user.MfaSecretBase32))
            throw new InvalidOperationException("MFA enrollment not started. Call /mfa/enroll first.");

        if (user.MfaEnabled)
            throw new InvalidOperationException("MFA is already confirmed.");

        if (!TotpService.Verify(user.MfaSecretBase32, totpCode))
            throw new UnauthorizedAccessException("Invalid TOTP code.");

        user.MfaEnabled = true;
        user.MfaEnabledAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);
    }
}
