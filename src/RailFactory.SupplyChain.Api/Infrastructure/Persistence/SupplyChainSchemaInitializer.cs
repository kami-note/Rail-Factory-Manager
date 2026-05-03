using Microsoft.EntityFrameworkCore;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainSchemaInitializer(IServiceProvider serviceProvider, ILogger<SupplyChainSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("SupplyChain schema migrated.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
