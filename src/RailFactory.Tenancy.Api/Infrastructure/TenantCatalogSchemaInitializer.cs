using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class TenantCatalogSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<TenantCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedDevTenantAsync(dbContext, cancellationToken);

        logger.LogInformation("Tenant catalog schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task SeedDevTenantAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        var tenant = Tenant.RegisterDevTenant();
        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == tenant.Code, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.Tenants.Add(new TenantRecord
            {
                Code = tenant.Code,
                DisplayName = tenant.DisplayName,
                Locale = tenant.Locale,
                TimeZone = tenant.TimeZone,
                Status = tenant.Status.ToString(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.DisplayName = tenant.DisplayName;
            existing.Locale = tenant.Locale;
            existing.TimeZone = tenant.TimeZone;
            existing.Status = tenant.Status.ToString();
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
