using Npgsql;
using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application;

public sealed class DeleteTenant(
    ITenantRepository tenants,
    IConfiguration configuration,
    ILogger<DeleteTenant> logger)
{
    private static readonly string[] ServiceDbs =
        ["iamdb", "supplychaindb", "inventorydb", "productiondb", "hrdb", "fleetdb", "logisticsdb"];

    public async Task<Result> ExecuteAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToLowerInvariant();

        var tenant = await tenants.FindByCodeAsync(normalized, cancellationToken);
        if (tenant is null)
            return Result.Failure(Error.NotFound("tenant.not_found", $"Tenant '{normalized}' não encontrado."));

        var serverCs = configuration.GetConnectionString("postgres")
            ?? throw new InvalidOperationException("Postgres server connection string not configured.");

        await DropDatabasesAsync(serverCs, normalized, cancellationToken);
        await tenants.RemoveAsync(tenant, cancellationToken);

        logger.LogInformation("Tenant '{TenantCode}' deleted along with all {Count} databases.", normalized, ServiceDbs.Length);
        return Result.Success();
    }

    private async Task DropDatabasesAsync(string serverCs, string tenantCode, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(serverCs);
        await conn.OpenAsync(cancellationToken);

        foreach (var db in ServiceDbs)
        {
            var dbName = $"tenant-{tenantCode}-{db}";

            // Terminate active connections so DROP DATABASE doesn't fail
            await using var terminateCmd = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1 AND pid <> pg_backend_pid()", conn);
            terminateCmd.Parameters.AddWithValue(dbName);
            await terminateCmd.ExecuteNonQueryAsync(cancellationToken);

            // Safe even if the database was never created (e.g. stale catalog record)
            await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\"", conn);
            await dropCmd.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Dropped database '{DatabaseName}'.", dbName);
        }
    }
}
