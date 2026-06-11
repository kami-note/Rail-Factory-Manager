using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Domain;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Tests;

public class SupplyChainSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<SupplyChainDbContext> _dbContextOptions;

    public SupplyChainSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        _dbContextOptions = new DbContextOptionsBuilder<SupplyChainDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new SupplyChainDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsSuppliersMappingsAndReceipts()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new SupplyChainDbContext(sp.GetRequiredService<DbContextOptions<SupplyChainDbContext>>()));
        
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
        var logger = Substitute.For<ILogger<SupplyChainSchemaInitializer>>();

        var initializer = new SupplyChainSchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Poll the database until receipts are seeded (up to 3 seconds) using a fresh context
        for (int i = 0; i < 30; i++)
        {
            using var checkContext = new SupplyChainDbContext(_dbContextOptions);
            if (await checkContext.Receipts.IgnoreQueryFilters().AnyAsync())
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
        using var assertContext = new SupplyChainDbContext(_dbContextOptions);
        
        var suppliers = await assertContext.Suppliers.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(suppliers);
        Assert.Contains(suppliers, s => s.Name == "ACME Metalúrgica Ltda");

        var mappings = await assertContext.SupplierMaterialMappings.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(mappings);
        Assert.Contains(mappings, m => m.SupplierProductCode == "SUP-ACO-2MM");

        var receipts = await assertContext.Receipts.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(receipts);
        Assert.Contains(receipts, r => r.ReceiptNumber == "NFE-00012345");
    }
}
