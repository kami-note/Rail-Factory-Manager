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
        bool isSqlite = connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

        if (isSqlite)
        {
            createCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS _rf_service_ready (
                    id  INTEGER PRIMARY KEY AUTOINCREMENT,
                    ready_at DATETIME NOT NULL
                );
                INSERT OR REPLACE INTO _rf_service_ready (id, ready_at)
                VALUES (1, datetime('now'));
                """;
        }
        else
        {
            createCmd.CommandText = """
                CREATE TABLE IF NOT EXISTS _rf_service_ready (
                    id  INT PRIMARY KEY DEFAULT 1,
                    ready_at TIMESTAMPTZ NOT NULL
                );
                INSERT INTO _rf_service_ready (id, ready_at)
                VALUES (1, NOW())
                ON CONFLICT (id) DO UPDATE SET ready_at = NOW();
                """;
        }
        await createCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Returns true only after <see cref="MarkReadyAsync"/> has been called for this database,
    /// i.e. all EF Core migrations have completed. Dispatchers call this before querying tenant
    /// data to avoid hitting tables that do not exist yet.
    /// </summary>
    public static async Task<bool> IsReadyAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT EXISTS (
                    SELECT 1 FROM _rf_service_ready WHERE id = 1
                );
                """;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value) return false;

            // SQLite returns 1/0 (long/int) for EXISTS, whereas PostgreSQL returns true/false (bool).
            // Convert.ToInt64 normalizes true -> 1 and 1 -> 1.
            return Convert.ToInt64(result) == 1;
        }
        catch
        {
            // Table does not exist, connection is not ready, or database is not reachable yet.
            return false;
        }
    }
}
