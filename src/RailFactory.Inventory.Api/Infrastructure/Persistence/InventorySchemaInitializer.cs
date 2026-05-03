using Microsoft.EntityFrameworkCore;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

public sealed class InventorySchemaInitializer(IServiceProvider serviceProvider, ILogger<InventorySchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Inventory schema migrated.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
