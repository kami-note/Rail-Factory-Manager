using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

public sealed class InboundWebhookProcessor(
    IServiceProvider serviceProvider,
    ILogger<InboundWebhookProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inbound webhook processor encountered an unexpected error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var activeTenants = await catalogClient.ListAllAsync(cancellationToken);

        foreach (var tenant in activeTenants.Where(t => t.IsActive))
        {
            try
            {
                await ProcessTenantBatchAsync(tenant, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("was not found in configuration"))
            {
                logger.LogDebug("Database for tenant {TenantCode} is not provisioned yet. Skipping webhook processing.", tenant.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inbound webhook processor failed for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task ProcessTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        // Set the tenant context so LogisticsDbContext resolves the correct connection string
        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var db = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();

        if (!await TenantServiceReadiness.IsReadyAsync(db.Database.GetDbConnection(), cancellationToken))
            return;

        var handlers = scope.ServiceProvider.GetServices<IInboundWebhookHandler>()
            .ToDictionary(h => h.Provider, StringComparer.OrdinalIgnoreCase);
        var repo = new PostgresInboundWebhookEventRepository(db);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var batch = await repo.GetPendingBatchAsync(tenant.Code, batchSize: 50, cancellationToken);

        foreach (var evt in batch)
        {
            if (!handlers.TryGetValue(evt.Provider, out var handler))
            {
                logger.LogWarning("No handler registered for webhook provider '{Provider}'.", evt.Provider);
                evt.MarkFailed($"No handler for provider '{evt.Provider}'.");
                await repo.UpdateAsync(evt, cancellationToken);
                continue;
            }

            try
            {
                await handler.HandleAsync(evt, cancellationToken);
                evt.MarkProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Handler for provider '{Provider}' failed on event {EventId} (attempt {Attempt}).",
                    evt.Provider, evt.Id, evt.RetryCount + 1);
                evt.MarkFailed(ex.Message);
            }

            await repo.UpdateAsync(evt, cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }
}
