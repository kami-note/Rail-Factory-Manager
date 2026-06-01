namespace RailFactory.Tenancy.Api.Domain;

public sealed class TenantIntegration
{
    private TenantIntegration(
        Guid id,
        string tenantId,
        string category,
        string providerType,
        bool isEnabled,
        byte[] encryptedCredentials,
        byte[] credentialsDek,
        byte[] credentialsIv,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        TenantId = tenantId;
        Category = category;
        ProviderType = providerType;
        IsEnabled = isEnabled;
        EncryptedCredentials = encryptedCredentials;
        CredentialsDek = credentialsDek;
        CredentialsIv = credentialsIv;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string Category { get; }
    public string ProviderType { get; private set; }
    public bool IsEnabled { get; private set; }
    public byte[] EncryptedCredentials { get; private set; }
    public byte[] CredentialsDek { get; private set; }
    public byte[] CredentialsIv { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateCredentials(string providerType, byte[] encryptedCredentials, byte[] credentialsDek, byte[] credentialsIv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerType);
        ProviderType = providerType;
        EncryptedCredentials = encryptedCredentials;
        CredentialsDek = credentialsDek;
        CredentialsIv = credentialsIv;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static TenantIntegration Create(
        string tenantId,
        string category,
        string providerType,
        byte[] encryptedCredentials,
        byte[] credentialsDek,
        byte[] credentialsIv)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerType);

        var now = DateTimeOffset.UtcNow;
        return new TenantIntegration(
            Guid.NewGuid(), tenantId, category, providerType,
            isEnabled: true,
            encryptedCredentials, credentialsDek, credentialsIv,
            now, now);
    }

    public static TenantIntegration Restore(
        Guid id, string tenantId, string category, string providerType,
        bool isEnabled, byte[] encryptedCredentials, byte[] credentialsDek,
        byte[] credentialsIv, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        return new TenantIntegration(id, tenantId, category, providerType,
            isEnabled, encryptedCredentials, credentialsDek, credentialsIv,
            createdAt, updatedAt);
    }
}
