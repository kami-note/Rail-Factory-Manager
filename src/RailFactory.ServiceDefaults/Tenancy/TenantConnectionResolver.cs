using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

internal sealed class TenantConnectionResolver(
    ITenantContextAccessor tenantContextAccessor,
    IConfiguration configuration) : ITenantConnectionResolver
{
    public string ResolveConnection(string serviceKey)
    {
        var tenantContext = tenantContextAccessor.Current;
        
        // 1. Try to get the connection string from the tenant context (Catalog-driven)
        if (tenantContext != null && tenantContext.ConnectionStrings.TryGetValue(serviceKey, out var connectionStringValue))
        {
            return ResolveValue(connectionStringValue);
        }

        // 2. Fallback to appsettings.json using the serviceKey as the connection string name
        var fallback = configuration.GetConnectionString(serviceKey);
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new InvalidOperationException($"Could not resolve connection string for service '{serviceKey}'. " +
            $"Ensure it is configured in the Tenant Catalog for tenant '{tenantContext?.TenantCode ?? "unknown"}' " +
            $"or in the application configuration.");
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

        // Try as a direct connection string name if it's just a simple string
        return configuration.GetConnectionString(value) ?? value;
    }
}
