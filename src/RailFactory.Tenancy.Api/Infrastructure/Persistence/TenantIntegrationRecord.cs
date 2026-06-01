namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class TenantIntegrationRecord
{
    public required Guid Id { get; init; }
    public required string TenantId { get; init; }
    public required string Category { get; init; }
    public required string ProviderType { get; set; }
    public required bool IsEnabled { get; set; }
    public required byte[] EncryptedCredentials { get; set; }
    public required byte[] CredentialsDek { get; set; }
    public required byte[] CredentialsIv { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }
}
