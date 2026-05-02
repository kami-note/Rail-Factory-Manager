using Npgsql;
using RailFactory.Iam.Api.Application.Auth;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

internal sealed class PostgresIamLocalUserRepository(NpgsqlDataSource dataSource) : IIamLocalUserRepository
{
    public async Task UpsertAsync(IamLocalUser user, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into iam_local_users (
                tenant_code,
                external_provider,
                external_subject,
                email,
                display_name
            )
            values (
                @tenantCode,
                @externalProvider,
                @externalSubject,
                @email,
                @displayName
            )
            on conflict (tenant_code, external_provider, external_subject) do update set
                email = excluded.email,
                display_name = excluded.display_name,
                last_login_at = now(),
                updated_at = now();
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("tenantCode", user.TenantCode);
        command.Parameters.AddWithValue("externalProvider", user.ExternalProvider);
        command.Parameters.AddWithValue("externalSubject", user.ExternalSubject);
        command.Parameters.AddWithValue("email", (object?)user.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("displayName", (object?)user.DisplayName ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
