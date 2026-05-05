using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainSchemaInitializer(IServiceProvider serviceProvider, ILogger<SupplyChainSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        await EnsureDefaultTenantContextAsync(scope.ServiceProvider, cancellationToken);
        var dbContext = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("SupplyChain schema migrated.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureDefaultTenantContextAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var options = services.GetRequiredService<IOptions<TenantRoutingOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.DefaultTenantCode))
        {
            throw new InvalidOperationException("TenantRouting:DefaultTenantCode is required for SupplyChain schema initialization.");
        }

        var catalogClient = services.GetRequiredService<ITenantCatalogClient>();
        var tenant = await catalogClient.ResolveAsync(options.DefaultTenantCode, cancellationToken);
        if (!tenant.Found || !tenant.IsActive)
        {
            throw new InvalidOperationException($"Default tenant '{options.DefaultTenantCode}' was not found or is inactive.");
        }

        var tenantContextAccessor = services.GetRequiredService<ITenantContextAccessor>();
        tenantContextAccessor.Current = new TenantContext(tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);
    }
}
