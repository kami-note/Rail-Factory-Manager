using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Iam.Api.Infrastructure.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Tests;

public class IamLocalUsersSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<IamAuthDbContext> _dbContextOptions;

    public IamLocalUsersSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        _dbContextOptions = new DbContextOptionsBuilder<IamAuthDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new IamAuthDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsRolesAndUsersAndUserRoles()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new IamAuthDbContext(
            sp.GetRequiredService<DbContextOptions<IamAuthDbContext>>(),
            sp.GetService<ITenantContextAccessor>()));
        
        var tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        tenantContextAccessor.Current.Returns(new TenantContext(
            "dev", "pt-BR", "America/Sao_Paulo", new Dictionary<string, string>()));
        services.AddSingleton(tenantContextAccessor);

        var client = Substitute.For<ITenantCatalogClient>();
        client.ListAllAsync(Arg.Any<CancellationToken>()).Returns(
            new List<TenantResolutionResult>
            {
                new(true, "dev", "pt-BR", "America/Sao_Paulo", true, new Dictionary<string, string>())
            });
        services.AddSingleton(client);

        var hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns(Environments.Development);

        var serviceProvider = services.BuildServiceProvider();
        var logger = Substitute.For<ILogger<IamLocalUsersSchemaInitializer>>();

        var initializer = new IamLocalUsersSchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        // Start the BackgroundService and run once
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Poll the database until user roles are seeded (up to 3 seconds) using a fresh context
        for (int i = 0; i < 30; i++)
        {
            using var checkContext = new IamAuthDbContext(_dbContextOptions);
            if (await checkContext.UserRoles.IgnoreQueryFilters().AnyAsync())
            {
                break;
            }
            await Task.Delay(100);
        }

        cts.Cancel(); // Stop the periodic timer loop

        if (initializer.ExecuteTask != null)
        {
            try
            {
                await initializer.ExecuteTask;
            }
            catch (Exception)
            {
                // Ignore cancellation exceptions
            }
        }

        // Assert
        // Roles should have been seeded
        using var assertContext = new IamAuthDbContext(_dbContextOptions);
        
        var roles = await assertContext.Roles.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(roles);
        Assert.Contains(roles, r => r.Name == "Administrador do Sistema");

        // Users should have been seeded
        var users = await assertContext.LocalUsers.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Email == "yurinote666@gmail.com");

        // User roles should have been mapped
        var userRoles = await assertContext.UserRoles.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(userRoles);
    }
}
