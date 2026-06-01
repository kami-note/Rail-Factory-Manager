using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application.Ports;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure;

namespace RailFactory.Tenancy.Api.Application;

public sealed class ConfigureIntegration(
    ITenantRepository tenants,
    ITenantIntegrationRepository integrations,
    CredentialEncryptionService encryption)
{
    public async Task<Result<Guid>> ExecuteAsync(
        string tenantId,
        string category,
        string providerType,
        Dictionary<string, string> credentials,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return Result<Guid>.Failure(Error.Validation("integration.tenant_required", "Tenant ID is required."));

        if (string.IsNullOrWhiteSpace(category))
            return Result<Guid>.Failure(Error.Validation("integration.category_required", "Category is required."));

        if (string.IsNullOrWhiteSpace(providerType))
            return Result<Guid>.Failure(Error.Validation("integration.provider_required", "Provider type is required."));

        var tenant = await tenants.FindByCodeAsync(tenantId, cancellationToken);
        if (tenant is null)
            return Result<Guid>.Failure(Error.NotFound("integration.tenant_not_found", "Tenant was not found."));

        var (ciphertext, wrappedDek, iv) = encryption.EncryptCredentials(credentials);

        var existing = await integrations.FindAsync(tenantId, category, cancellationToken);
        try
        {
            if (existing is null)
            {
                var integration = TenantIntegration.Create(tenantId, category, providerType, ciphertext, wrappedDek, iv);
                await integrations.AddAsync(integration, cancellationToken);
                return Result<Guid>.Success(integration.Id);
            }

            existing.UpdateCredentials(providerType, ciphertext, wrappedDek, iv);
            await integrations.UpdateAsync(existing, cancellationToken);
            return Result<Guid>.Success(existing.Id);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Result<Guid>.Failure(
                Error.Conflict("integration.already_exists", "A concurrent request already configured this integration."));
        }
    }
}
