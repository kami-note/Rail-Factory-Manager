using Npgsql;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class PostgresTenantRepository(NpgsqlDataSource dataSource) : ITenantRepository
{
    public Task<Tenant?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync(id, cancellationToken);
    }

    public async Task<Tenant?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select code, display_name, locale, time_zone, status
            from tenants
            where lower(code) = lower(@code)
            limit 1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", code);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Tenant.Restore(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<TenantStatus>(reader.GetString(4), ignoreCase: true));
    }

    public async Task AddAsync(Tenant aggregate, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into tenants (code, display_name, locale, time_zone, status)
            values (@code, @displayName, @locale, @timeZone, @status)
            on conflict (code) do update set
                display_name = excluded.display_name,
                locale = excluded.locale,
                time_zone = excluded.time_zone,
                status = excluded.status,
                updated_at = now();
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", aggregate.Code);
        command.Parameters.AddWithValue("displayName", aggregate.DisplayName);
        command.Parameters.AddWithValue("locale", aggregate.Locale);
        command.Parameters.AddWithValue("timeZone", aggregate.TimeZone);
        command.Parameters.AddWithValue("status", aggregate.Status.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
