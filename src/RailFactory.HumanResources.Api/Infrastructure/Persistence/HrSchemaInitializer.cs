using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class HrSchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<HrSchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HR schema initializer started.");
        await MigrateNewTenantsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await MigrateNewTenantsAsync(stoppingToken);
    }

    private async Task MigrateNewTenantsAsync(CancellationToken cancellationToken)
    {
        List<TenantResolutionResult> pending;
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
            var all = await client.ListAllAsync(cancellationToken);
            pending = all.Where(t => t.IsActive && !_migratedTenants.Contains(t.Code)).ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not fetch tenant list from catalog. Will retry in 15s.");
            return;
        }

        if (pending.Count == 0) return;

        logger.LogInformation("Migrating HR databases for {Count} new tenant(s)...", pending.Count);
        await Task.WhenAll(pending.Select(t => MigrateTenantAsync(t, cancellationToken)));
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<HrDbContext>();
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            await SeedHrDataAsync(dbContext, tenant.Code, cancellationToken);
            await TenantServiceReadiness.MarkReadyAsync(dbContext.Database.GetDbConnection(), cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("HR database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate HR database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    private async Task SeedHrDataAsync(
        HrDbContext dbContext,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Carlos Silva (Employee)
        var carlos = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "01234567890", cancellationToken);
        if (carlos == null)
        {
            carlos = Person.Create("Carlos Silva", "01234567890", PersonType.Employee, "carlos@railfactory.com.br", id: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            dbContext.People.Add(carlos);
        }

        // 2. Ana Souza (Employee)
        var ana = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "98765432100", cancellationToken);
        if (ana == null)
        {
            ana = Person.Create("Ana Souza", "98765432100", PersonType.Employee, "ana@railfactory.com.br", id: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            dbContext.People.Add(ana);
        }

        // 3. Marcos Oliveira (Driver)
        var marcos = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "45678912300", cancellationToken);
        if (marcos == null)
        {
            marcos = Person.Create("Marcos Oliveira", "45678912300", PersonType.Driver, "marcos@railfactory.com.br", id: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            dbContext.People.Add(marcos);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
