using Npgsql;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class TenantCatalogSchemaInitializer(
    NpgsqlDataSource dataSource,
    ILogger<TenantCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateSchemaAsync(cancellationToken);
        await SeedDevTenantAsync(cancellationToken);

        logger.LogInformation("Tenant catalog schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists tenants (
                code text primary key,
                display_name text not null,
                locale text not null,
                time_zone text not null,
                status text not null,
                created_at timestamptz not null default now(),
                updated_at timestamptz not null default now()
            );

            create index if not exists ix_tenants_status on tenants (status);
            """;

        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task SeedDevTenantAsync(CancellationToken cancellationToken)
    {
        var tenant = Tenant.RegisterDevTenant();

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
        command.Parameters.AddWithValue("code", tenant.Code);
        command.Parameters.AddWithValue("displayName", tenant.DisplayName);
        command.Parameters.AddWithValue("locale", tenant.Locale);
        command.Parameters.AddWithValue("timeZone", tenant.TimeZone);
        command.Parameters.AddWithValue("status", tenant.Status.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
