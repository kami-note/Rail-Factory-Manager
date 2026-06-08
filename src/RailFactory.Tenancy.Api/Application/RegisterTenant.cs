using Npgsql;
using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Application;

public sealed record RegisterTenantInput(
    string Code,
    string DisplayName,
    string Locale = "pt-BR",
    string TimeZone = "America/Sao_Paulo");

public sealed class RegisterTenant(
    ITenantRepository tenants,
    IConfiguration configuration,
    ILogger<RegisterTenant> logger)
{
    private static readonly string[] ServiceDbs =
        ["iamdb", "supplychaindb", "inventorydb", "productiondb", "hrdb", "fleetdb", "logisticsdb"];

    public async Task<Result<TenantDetails>> ExecuteAsync(
        RegisterTenantInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Code)
            || !System.Text.RegularExpressions.Regex.IsMatch(input.Code, @"^[a-z0-9\-]{2,50}$"))
            return Result<TenantDetails>.Failure(
                Error.Validation("tenant.invalid_code",
                    "Código deve ter 2-50 caracteres: letras minúsculas, números e hífens."));

        var normalizedCode = input.Code.Trim().ToLowerInvariant();

        var existing = await tenants.FindByCodeAsync(normalizedCode, cancellationToken);
        if (existing is not null)
            return Result<TenantDetails>.Failure(
                Error.Conflict("tenant.already_exists", $"Tenant '{normalizedCode}' já existe."));

        var serverCs = configuration.GetConnectionString("postgres")
            ?? throw new InvalidOperationException(
                "Postgres server connection string ('ConnectionStrings:postgres') not configured. " +
                "Ensure tenant-management has WithReference(postgres) in the AppHost.");

        var connectionStrings = await ProvisionDatabasesAsync(serverCs, normalizedCode, cancellationToken);

        var tenant = Tenant.Register(
            normalizedCode, input.DisplayName.Trim(),
            input.Locale.Trim(), input.TimeZone.Trim(),
            connectionStrings);

        await tenants.AddAsync(tenant, cancellationToken);

        logger.LogInformation("Tenant '{TenantCode}' registered with {Count} databases.", normalizedCode, ServiceDbs.Length);

        return Result<TenantDetails>.Success(new TenantDetails(
            tenant.Code, tenant.DisplayName, tenant.Locale, tenant.TimeZone,
            tenant.Status.ToDisplayStatus(), tenant.ConnectionStrings));
    }

    private async Task<IReadOnlyDictionary<string, string>> ProvisionDatabasesAsync(
        string serverCs, string tenantCode, CancellationToken cancellationToken)
    {
        var connectionStrings = new Dictionary<string, string>();

        await using var conn = new NpgsqlConnection(serverCs);
        await conn.OpenAsync(cancellationToken);

        foreach (var db in ServiceDbs)
        {
            var dbName = $"tenant-{tenantCode}-{db}";

            await using var checkCmd = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = $1", conn);
            checkCmd.Parameters.AddWithValue(dbName);
            var exists = await checkCmd.ExecuteScalarAsync(cancellationToken) is not null;

            if (!exists)
            {
                // CREATE DATABASE cannot be parameterized; name is safe: validated ^[a-z0-9\-]{2,50}$ + hardcoded suffix
                await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn);
                await createCmd.ExecuteNonQueryAsync(cancellationToken);
                logger.LogInformation("Created database '{DatabaseName}'.", dbName);
            }
            else
            {
                logger.LogInformation("Database '{DatabaseName}' already exists, skipping creation.", dbName);
            }

            connectionStrings[db] = new NpgsqlConnectionStringBuilder(serverCs) { Database = dbName }.ConnectionString;
        }

        return connectionStrings;
    }
}
