using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class TenantConnectionResolver(
    ITenantContextAccessor tenantContextAccessor,
    IConfiguration configuration) : ITenantConnectionResolver
{
    public string ResolveConnection(string serviceKey)
    {
        var tenantContext = tenantContextAccessor.Current;

        if (tenantContext is null)
        {
            throw new InvalidOperationException(
                $"Could not resolve connection string for service '{serviceKey}' because tenant context is not available. " +
                "Ensure tenant resolution was executed before database access.");
        }

        if (tenantContext.ConnectionStrings.TryGetValue(serviceKey, out var connectionStringValue))
        {
            return ResolveValue(connectionStringValue);
        }

        throw new InvalidOperationException($"Could not resolve connection string for service '{serviceKey}'. " +
            $"Ensure it is configured in the Tenant Catalog for tenant '{tenantContext.TenantCode}'.");
    }

    private string ResolveValue(string value)
    {
        // Support for environment variable placeholders or Aspire-style names stored in the DB
        if (value.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase))
        {
            var key = value.Substring("ConnectionStrings:".Length);
            return configuration.GetConnectionString(key)
                ?? throw new InvalidOperationException($"Catalog pointed to connection string '{key}', but it was not found in configuration.");
        }

        // If it doesn't look like a reference, treat it as the literal connection string
        if (value.Contains('=') || value.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        // Treat as an Aspire/config key name — throw if not found so callers get a clear
        // error instead of Npgsql failing to parse the raw name as a connection string.
        return configuration.GetConnectionString(value)
            ?? throw new InvalidOperationException(
                $"Connection string '{value}' was not found in configuration. " +
                "Ensure the database is provisioned and referenced in the Aspire AppHost (or configuration) for this service.");
    }
}
