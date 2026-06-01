using System.Security.Cryptography;

namespace RailFactory.Iam.Api.Domain;

/// <summary>
/// Represents an API key for machine-to-machine integrations (RF-06).
/// The plaintext key is returned only at creation; only the SHA-256 hash is stored.
/// </summary>
public sealed class IamApiKey
{
    public Guid Id { get; private set; }
    public string TenantCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>First 8 characters of the key for display/identification.</summary>
    public string KeyPrefix { get; private set; } = string.Empty;

    /// <summary>SHA-256 hex hash of the full plaintext key.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>JSON array of permission strings granted to this key.</summary>
    public string PermissionsJson { get; private set; } = "[]";

    public string CreatedByEmail { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsActive => RevokedAt is null && (ExpiresAt is null || ExpiresAt > DateTimeOffset.UtcNow);

    private IamApiKey() { }

    /// <summary>
    /// Generates a new key. Returns the entity and the single-use plaintext key.
    /// Format: <c>rfk_&lt;base64url-32-bytes&gt;</c>
    /// </summary>
    public static (IamApiKey Entity, string PlaintextKey) Generate(
        string tenantCode, string name, string createdByEmail,
        string permissionsJson, DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByEmail);

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var plaintextKey = $"rfk_{Convert.ToBase64String(rawBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
        var keyHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintextKey)));

        var entity = new IamApiKey
        {
            Id = Guid.NewGuid(),
            TenantCode = tenantCode,
            Name = name.Trim(),
            KeyPrefix = plaintextKey[..Math.Min(12, plaintextKey.Length)],
            KeyHash = keyHash,
            PermissionsJson = permissionsJson,
            CreatedByEmail = createdByEmail.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };

        return (entity, plaintextKey);
    }

    public void Revoke()
    {
        if (RevokedAt is not null) throw new InvalidOperationException("API key is already revoked.");
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
