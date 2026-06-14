using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Domain;
using RailFactory.Inventory.Api.Infrastructure.Persistence;

namespace RailFactory.Inventory.Api.Tests;

public class InventorySchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<InventoryDbContext> _dbContextOptions;
    private readonly AuditSaveChangesInterceptor _auditInterceptor;

    public InventorySchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        _dbContextOptions = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        var userContext = Substitute.For<IUserContext>();
        userContext.Email.Returns(EmailAddress.From("admin@railfactory.com.br"));
        _auditInterceptor = new AuditSaveChangesInterceptor(userContext);

        // Ensure schema is created once on the shared connection
        using var setupContext = new InventoryDbContext(_dbContextOptions, _auditInterceptor);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsMaterialsAndBalances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddSingleton(_auditInterceptor);
        services.AddScoped(sp => new InventoryDbContext(
            sp.GetRequiredService<DbContextOptions<InventoryDbContext>>(),
            sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        
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
        var logger = Substitute.For<ILogger<InventorySchemaInitializer>>();

        var initializer = new InventorySchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Poll the database until inventory balances are seeded (up to 3 seconds) using a fresh context
        for (int i = 0; i < 30; i++)
        {
            using var checkContext = new InventoryDbContext(_dbContextOptions, _auditInterceptor);
            if (await checkContext.Balances.IgnoreQueryFilters().AnyAsync())
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
        using var assertContext = new InventoryDbContext(_dbContextOptions, _auditInterceptor);
        
        var stockLocations = await assertContext.StockLocations.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(stockLocations);
        Assert.Contains(stockLocations, sl => sl.Code == "ALM-CENTRAL");

        var materials = await assertContext.Materials.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(materials);
        Assert.Contains(materials, m => m.MaterialCode == MaterialCode.From("MAT-ACO-2MM"));
        Assert.Contains(materials, m => m.MaterialCode == MaterialCode.From("MAT-PAR-M8"));
        Assert.Contains(materials, m => m.MaterialCode == MaterialCode.From("PRO-TR-100"));

        var balances = await assertContext.Balances.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(balances);
        Assert.Contains(balances, b => b.MaterialCode == "MAT-ACO-2MM" && b.Quantity == 1200.0m && b.Status == InventoryBalanceStatus.Available);
        Assert.Contains(balances, b => b.MaterialCode == "MAT-PAR-M8" && b.Quantity == 5000.0m && b.Status == InventoryBalanceStatus.Available);
        Assert.Contains(balances, b => b.MaterialCode == "PRO-TR-100" && b.Quantity == 25.0m && b.Status == InventoryBalanceStatus.Available);
    }
}
