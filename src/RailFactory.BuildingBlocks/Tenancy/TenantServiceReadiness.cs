using System.Data;
using System.Data.Common;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Writes a sentinel marker after all EF Core migrations complete so that external
/// readiness checks don't falsely report "ready" while migrations are still running.
/// </summary>
public static class TenantServiceReadiness
{
    public static async Task MarkReadyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var createCmd = connection.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS _rf_service_ready (
                id  INT PRIMARY KEY DEFAULT 1,
                ready_at TIMESTAMPTZ NOT NULL
            );
            INSERT INTO _rf_service_ready (id, ready_at)
            VALUES (1, NOW())
            ON CONFLICT (id) DO UPDATE SET ready_at = NOW();
            """;
        await createCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Returns true only after <see cref="MarkReadyAsync"/> has been called for this database,
    /// i.e. all EF Core migrations have completed. Dispatchers call this before querying tenant
    /// data to avoid hitting tables that do not exist yet.
    /// </summary>
    public static async Task<bool> IsReadyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM _rf_service_ready WHERE id = 1
            );
            """;

        try
        {
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result is true;
        }
        catch
        {
            // Table does not exist yet — migrations haven't run.
            return false;
        }
    }
}
