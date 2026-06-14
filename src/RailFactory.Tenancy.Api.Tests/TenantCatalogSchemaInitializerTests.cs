using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Infrastructure;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Tests;

public class TenantCatalogSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly TenancyDbContext _dbContext;

    public TenantCatalogSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:;Busy Timeout=5000");
        _sqliteConnection.Open();

        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _dbContext = new TenancyDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task StartAsync_WhenInDevelopmentAndTenantsDoNotExist_AttemptsToRegisterTenants()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);

        var hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns(Environments.Development);

        var config = Substitute.For<IConfiguration>();
        config.GetSection("ConnectionStrings")["postgres"].Returns("Host=localhost;Database=postgres;Username=postgres;Password=postgres");

        var tenantRepo = Substitute.For<ITenantRepository>();
        tenantRepo.FindByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(null));

        // Since RegisterTenant is concrete and sealed, we will stub its dependencies.
        // It will fail inside ExecuteAsync due to trying to open NpgsqlConnection, but we can verify that 
        // the logger logs "Seeding tenant 'dev'..." and "Seeding tenant 'acme'..." before trying to connect.
        var mockLogger = Substitute.For<ILogger<RegisterTenant>>();
        var registerTenant = new RegisterTenant(tenantRepo, config, mockLogger);
        services.AddSingleton(registerTenant);

        var serviceProvider = services.BuildServiceProvider();
        var initializerLogger = Substitute.For<ILogger<TenantCatalogSchemaInitializer>>();

        var initializer = new TenantCatalogSchemaInitializer(serviceProvider, hostEnv, initializerLogger);

        // Act
        await initializer.StartAsync(CancellationToken.None);

        // Assert
        // The initializer should log info about seeding tenants.
        initializerLogger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Seeding tenant 'dev'")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        initializerLogger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Seeding tenant 'acme'")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task StartAsync_WhenNotInDevelopment_DoesNotAttemptToRegisterTenants()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContext);

        var hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns(Environments.Production);

        var serviceProvider = services.BuildServiceProvider();
        var initializerLogger = Substitute.For<ILogger<TenantCatalogSchemaInitializer>>();

        var initializer = new TenantCatalogSchemaInitializer(serviceProvider, hostEnv, initializerLogger);

        // Act
        await initializer.StartAsync(CancellationToken.None);

        // Assert
        // Initializer logger should not contain seeding tenant logs
        initializerLogger.DidNotReceive().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Seeding tenant")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
