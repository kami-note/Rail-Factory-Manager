using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class PostgresTenantRepository(TenancyDbContext dbContext) : ITenantRepository
{
    public Task<Tenant?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync(id, cancellationToken);
    }

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

    public async Task AddAsync(Tenant aggregate, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == aggregate.Code, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.Tenants.Add(new TenantRecord
            {
                Code = aggregate.Code,
                DisplayName = aggregate.DisplayName,
                Locale = aggregate.Locale,
                TimeZone = aggregate.TimeZone,
                Status = aggregate.Status.ToString(),
                ConnectionStrings = aggregate.ConnectionStrings.ToDictionary(x => x.Key, x => x.Value),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.DisplayName = aggregate.DisplayName;
            existing.Locale = aggregate.Locale;
            existing.TimeZone = aggregate.TimeZone;
            existing.Status = aggregate.Status.ToString();
            existing.ConnectionStrings = aggregate.ConnectionStrings.ToDictionary(x => x.Key, x => x.Value);
            existing.UpdatedAt = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
