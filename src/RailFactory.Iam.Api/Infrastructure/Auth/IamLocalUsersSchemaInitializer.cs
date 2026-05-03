using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

public sealed class IamLocalUsersSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<IamLocalUsersSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IamAuthDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("IAM local users schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
