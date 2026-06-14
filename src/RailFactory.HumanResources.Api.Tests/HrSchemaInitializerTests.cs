using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.HumanResources.Api.Domain;
using RailFactory.HumanResources.Api.Infrastructure.Persistence;

namespace RailFactory.HumanResources.Api.Tests;

public class HrSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<HrDbContext> _dbContextOptions;

    public HrSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:;Busy Timeout=5000");
        _sqliteConnection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<HrDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new HrDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsHrData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new HrDbContext(
            sp.GetRequiredService<DbContextOptions<HrDbContext>>()));
        
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
        var logger = Substitute.For<ILogger<HrSchemaInitializer>>();

        var initializer = new HrSchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Poll the database until people are seeded (up to 3 seconds)
        for (int i = 0; i < 30; i++)
        {
            try
            {
                using var checkContext = new HrDbContext(_dbContextOptions);
                if (await checkContext.People.IgnoreQueryFilters().CountAsync() >= 3)
                {
                    break;
                }
            }
            catch (Exception)
            {
                // Ignore transient SQLite connection lock exceptions during concurrent seeding writes
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
        using var assertContext = new HrDbContext(_dbContextOptions);
        
        var people = await assertContext.People.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(people);
        
        var carlos = people.FirstOrDefault(p => p.DocumentNumber == "01234567890");
        Assert.NotNull(carlos);
        Assert.Equal("Carlos Silva", carlos.Name);
        Assert.Equal(PersonType.Employee, carlos.Type);
        Assert.Equal(PersonStatus.Active, carlos.Status);

        var ana = people.FirstOrDefault(p => p.DocumentNumber == "98765432100");
        Assert.NotNull(ana);
        Assert.Equal("Ana Souza", ana.Name);
        Assert.Equal(PersonType.Employee, ana.Type);
        Assert.Equal(PersonStatus.Active, ana.Status);

        var marcos = people.FirstOrDefault(p => p.DocumentNumber == "45678912300");
        Assert.NotNull(marcos);
        Assert.Equal("Marcos Oliveira", marcos.Name);
        Assert.Equal(PersonType.Driver, marcos.Type);
        Assert.Equal(PersonStatus.Active, marcos.Status);
    }
}
