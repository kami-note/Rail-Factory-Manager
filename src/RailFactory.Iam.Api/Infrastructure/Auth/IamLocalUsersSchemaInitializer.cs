using Npgsql;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

public sealed class IamLocalUsersSchemaInitializer(
    NpgsqlDataSource dataSource,
    ILogger<IamLocalUsersSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            create table if not exists iam_local_users (
                tenant_code text not null,
                external_provider text not null,
                external_subject text not null,
                email text null,
                display_name text null,
                first_login_at timestamptz not null default now(),
                last_login_at timestamptz not null default now(),
                updated_at timestamptz not null default now(),
                primary key (tenant_code, external_provider, external_subject)
            );

            create index if not exists ix_iam_local_users_tenant_email
                on iam_local_users (tenant_code, email);
            """;

        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("IAM local users schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
