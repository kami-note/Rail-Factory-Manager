using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class PostgresTenantRepository(TenancyDbContext dbContext) : ITenantRepository
{
    public async Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        var record = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, cancellationToken);
        if (record is null)
        {
            return null;
        }

        return Tenant.Restore(
            record.Code,
            record.DisplayName,
            record.Locale,
            record.TimeZone,
            Enum.Parse<TenantStatus>(record.Status, ignoreCase: true),
            record.ConnectionStrings);
    }

    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records.Select(record => Tenant.Restore(
            record.Code,
            record.DisplayName,
            record.Locale,
            record.TimeZone,
            Enum.Parse<TenantStatus>(record.Status, ignoreCase: true),
            record.ConnectionStrings)).ToList();
    }

}
