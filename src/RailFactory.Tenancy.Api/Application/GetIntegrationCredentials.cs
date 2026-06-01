using RailFactory.BuildingBlocks.Integrations;
using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application.Ports;
using RailFactory.Tenancy.Api.Infrastructure;

namespace RailFactory.Tenancy.Api.Application;

public sealed class GetIntegrationCredentials(
    ITenantIntegrationRepository integrations,
    CredentialEncryptionService encryption)
{
    public async Task<Result<IntegrationCredentialsDetails>> ExecuteAsync(
        string tenantId,
        string category,
        CancellationToken cancellationToken = default)
    {
        var integration = await integrations.FindAsync(tenantId, category, cancellationToken);
        if (integration is null)
            return Result<IntegrationCredentialsDetails>.Failure(
                Error.NotFound("integration.not_found", "Integration not configured for this tenant and category."));

        if (!integration.IsEnabled)
            return Result<IntegrationCredentialsDetails>.Failure(
                Error.NotFound("integration.not_available", "Integration is disabled for this tenant."));

        var credentials = encryption.DecryptCredentials(
            integration.EncryptedCredentials,
            integration.CredentialsDek,
            integration.CredentialsIv);

        return Result<IntegrationCredentialsDetails>.Success(
            new IntegrationCredentialsDetails(integration.ProviderType, credentials));
    }
}

public sealed class IntegrationCredentialsDetails(string providerType, SecureCredentials credentials)
    : IDisposable
{
    public string ProviderType { get; } = providerType;
    public SecureCredentials Credentials { get; } = credentials;

    public void Dispose() => Credentials.Dispose();
}
