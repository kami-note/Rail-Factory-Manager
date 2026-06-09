using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Infrastructure;

/// <summary>
/// A PostgreSQL-backed repository for managing tenant configuration data.
/// </summary>
public sealed class PostgresTenantRepository(
    TenancyDbContext dbContext,
    IConfiguration configuration) : ITenantRepository
{
    /// <inheritdoc />
    public async Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        var record = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, cancellationToken);

        return record is null ? null : ToTenant(record);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tenant>> ListTenantsAsync(CancellationToken cancellationToken = default)
    {
        var records = await dbContext.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        return records.Select(ToTenant).ToList();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task RemoveAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == tenant.Code, cancellationToken);
        if (record is null) return;
        dbContext.Tenants.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Converts a <see cref="TenantRecord"/> from persistence to a <see cref="Tenant"/> domain model,
    /// dynamically rewriting localhost database connections to match the current PostgreSQL server port.
    /// </summary>
    /// <remarks>
    /// In local development (Aspire environments), the Postgres container port is assigned dynamically
    /// on host machine restarts. Since the tenant catalog database resides in a persistent docker volume,
    /// stored tenant connection strings may point to stale ports. This mapper dynamically corrects
    /// localhost connection strings to use the current port, host, and credentials from environment config.
    /// </remarks>
    private Tenant ToTenant(TenantRecord r)
    {
        var connectionStrings = new Dictionary<string, string>();
        var serverCs = configuration.GetConnectionString("postgres");
        NpgsqlConnectionStringBuilder? serverBuilder = null;

        if (!string.IsNullOrWhiteSpace(serverCs))
        {
            try
            {
                serverBuilder = new NpgsqlConnectionStringBuilder(serverCs);
            }
            catch
            {
                // Ignore parsing errors for the server connection string to prevent startup crashes
            }
        }

        foreach (var kvp in r.ConnectionStrings)
        {
            var connStr = kvp.Value;
            if (serverBuilder != null && (connStr.Contains("localhost", StringComparison.OrdinalIgnoreCase) || connStr.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var builder = new NpgsqlConnectionStringBuilder(connStr);
                    builder.Host = serverBuilder.Host;
                    builder.Port = serverBuilder.Port;
                    builder.Username = serverBuilder.Username;
                    builder.Password = serverBuilder.Password;
                    connStr = builder.ConnectionString;
                }
                catch
                {
                    // Fallback to original connection string if parsing fails
                }
            }
            connectionStrings[kvp.Key] = connStr;
        }

        return Tenant.Restore(
            r.Code,
            r.DisplayName,
            r.Locale,
            r.TimeZone,
            Enum.Parse<TenantStatus>(r.Status, ignoreCase: true),
            connectionStrings);
    }
}
