using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Mfa;

/// <summary>
/// Generates a new TOTP secret for the user and returns the enrollment URI (RF-07).
/// The user must confirm with a valid TOTP code before MFA is marked enabled.
/// </summary>
public sealed class EnrollMfa(IamAuthDbContext dbContext)
{
    public async Task<MfaEnrollmentResult> ExecuteAsync(string provider, string subject, string email, CancellationToken ct)
    {
        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.ExternalProvider == provider && u.ExternalSubject == subject, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.MfaEnabled)
            throw new InvalidOperationException("MFA is already enabled. Disable it first.");

        user.MfaSecretBase32 = TotpService.GenerateSecretBase32();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        var uri = TotpService.GetOtpAuthUri(user.MfaSecretBase32, email);
        return new MfaEnrollmentResult(user.MfaSecretBase32, uri);
    }
}

public sealed record MfaEnrollmentResult(string SecretBase32, string OtpAuthUri);
