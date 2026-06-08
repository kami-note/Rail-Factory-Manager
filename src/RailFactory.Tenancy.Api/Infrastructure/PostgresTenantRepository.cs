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

        return record is null ? null : ToTenant(record);
    }

    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        return records.Select(ToTenant).ToList();
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        dbContext.Tenants.Add(new TenantRecord
        {
            Code = tenant.Code,
            DisplayName = tenant.DisplayName,
            Locale = tenant.Locale,
            TimeZone = tenant.TimeZone,
            Status = tenant.Status.ToString(),
            ConnectionStrings = tenant.ConnectionStrings.ToDictionary(k => k.Key, v => v.Value),
            CreatedAt = now,
            UpdatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == tenant.Code, cancellationToken);
        if (record is null) return;
        dbContext.Tenants.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Tenant ToTenant(TenantRecord r) =>
        Tenant.Restore(r.Code, r.DisplayName, r.Locale, r.TimeZone,
            Enum.Parse<TenantStatus>(r.Status, ignoreCase: true), r.ConnectionStrings);
}
