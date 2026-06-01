using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Application.Ports;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class PostgresTenantIntegrationRepository(TenancyDbContext db) : ITenantIntegrationRepository
{
    public async Task AddAsync(TenantIntegration integration, CancellationToken cancellationToken = default)
    {
        db.TenantIntegrations.Add(new TenantIntegrationRecord
        {
            Id = integration.Id,
            TenantId = integration.TenantId,
            Category = integration.Category,
            ProviderType = integration.ProviderType,
            IsEnabled = integration.IsEnabled,
            EncryptedCredentials = integration.EncryptedCredentials,
            CredentialsDek = integration.CredentialsDek,
            CredentialsIv = integration.CredentialsIv,
            CreatedAt = integration.CreatedAt,
            UpdatedAt = integration.UpdatedAt
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TenantIntegration integration, CancellationToken cancellationToken = default)
    {
        await db.TenantIntegrations
            .Where(r => r.Id == integration.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ProviderType, integration.ProviderType)
                .SetProperty(r => r.IsEnabled, integration.IsEnabled)
                .SetProperty(r => r.EncryptedCredentials, integration.EncryptedCredentials)
                .SetProperty(r => r.CredentialsDek, integration.CredentialsDek)
                .SetProperty(r => r.CredentialsIv, integration.CredentialsIv)
                .SetProperty(r => r.UpdatedAt, integration.UpdatedAt),
            cancellationToken);
    }

    public async Task<TenantIntegration?> FindAsync(
        string tenantId, string category, CancellationToken cancellationToken = default)
    {
        var record = await db.TenantIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId && r.Category == category, cancellationToken);

        return record is null ? null : MapToDomain(record);
    }

    public async Task<IReadOnlyList<TenantIntegration>> ListByTenantAsync(
        string tenantId, CancellationToken cancellationToken = default)
    {
        var records = await db.TenantIntegrations
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Category)
            .ToListAsync(cancellationToken);

        return records.Select(MapToDomain).ToList();
    }

    private static TenantIntegration MapToDomain(TenantIntegrationRecord r) =>
        TenantIntegration.Restore(
            r.Id, r.TenantId, r.Category, r.ProviderType,
            r.IsEnabled, r.EncryptedCredentials, r.CredentialsDek,
            r.CredentialsIv, r.CreatedAt, r.UpdatedAt);
}
