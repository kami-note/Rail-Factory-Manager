using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Domain;
using RailFactory.Production.Api.Infrastructure.Persistence;

namespace RailFactory.Production.Api.Tests;

public class ProductionSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<ProductionDbContext> _dbContextOptions;

    public ProductionSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        _dbContextOptions = new DbContextOptionsBuilder<ProductionDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new ProductionDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsProductionData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new ProductionDbContext(
            sp.GetRequiredService<DbContextOptions<ProductionDbContext>>()));
        
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
        var logger = Substitute.For<ILogger<ProductionSchemaInitializer>>();

        var initializer = new ProductionSchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Poll the database until all 3 production orders are seeded (up to 3 seconds)
        for (int i = 0; i < 30; i++)
        {
            try
            {
                using var checkContext = new ProductionDbContext(_dbContextOptions);
                if (await checkContext.ProductionOrders.IgnoreQueryFilters().CountAsync() >= 3)
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
        using var assertContext = new ProductionDbContext(_dbContextOptions);
        
        var workCenters = await assertContext.WorkCenters.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(workCenters);
        Assert.Contains(workCenters, wc => wc.Code == "WC-COR-01");
        Assert.Contains(workCenters, wc => wc.Code == "WC-SOL-02");
        Assert.Contains(workCenters, wc => wc.Code == "WC-MON-03");

        var boms = await assertContext.Boms.Include(b => b.Items).IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(boms);
        var bom1 = boms.FirstOrDefault(b => b.ProductCode == MaterialCode.From("PRO-TR-100") && b.Version == 1);
        Assert.NotNull(bom1);
        Assert.Equal(BomStatus.Active, bom1.Status);
        Assert.Equal(2, bom1.Items.Count);
        Assert.Contains(bom1.Items, item => item.MaterialCode == MaterialCode.From("MAT-ACO-2MM") && item.Quantity == 15.5m);
        Assert.Contains(bom1.Items, item => item.MaterialCode == MaterialCode.From("MAT-PAR-M8") && item.Quantity == 8.0m);

        var orders = await assertContext.ProductionOrders.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(orders);

        var opCheck1 = orders.FirstOrDefault(o => o.OrderNumber == "OP-2026-0001");
        Assert.NotNull(opCheck1);
        Assert.Equal(ProductionOrderStatus.Completed, opCheck1.Status);
        Assert.Equal(10m, opCheck1.PlannedQuantity);

        var opCheck2 = orders.FirstOrDefault(o => o.OrderNumber == "OP-2026-0002");
        Assert.NotNull(opCheck2);
        Assert.Equal(ProductionOrderStatus.Released, opCheck2.Status);
        Assert.Equal(25m, opCheck2.PlannedQuantity);

        var opCheck3 = orders.FirstOrDefault(o => o.OrderNumber == "OP-2026-0003");
        Assert.NotNull(opCheck3);
        Assert.Equal(ProductionOrderStatus.Draft, opCheck3.Status);
        Assert.Equal(50m, opCheck3.PlannedQuantity);

        var op1 = orders.First(o => o.OrderNumber == "OP-2026-0001");
        
        var inspections = await assertContext.QualityInspections.IgnoreQueryFilters().Where(x => x.ProductionOrderId == op1.Id).ToListAsync();
        Assert.NotEmpty(inspections);
        Assert.Contains(inspections, ins => ins.Result == InspectionResult.Passed && ins.InspectedBy == "Carlos Silva");

        var consumptions = await assertContext.ConsumptionRecords.IgnoreQueryFilters().Where(x => x.ProductionOrderId == op1.Id).ToListAsync();
        Assert.Equal(2, consumptions.Count);
        Assert.Contains(consumptions, c => c.MaterialCode == MaterialCode.From("MAT-ACO-2MM") && c.ConsumedQuantity == 155.0m);
        Assert.Contains(consumptions, c => c.MaterialCode == MaterialCode.From("MAT-PAR-M8") && c.ConsumedQuantity == 80.0m);

        var scraps = await assertContext.ScrapRecords.IgnoreQueryFilters().Where(x => x.ProductionOrderId == op1.Id).ToListAsync();
        Assert.Single(scraps);
        Assert.Contains(scraps, s => s.MaterialCode == MaterialCode.From("MAT-ACO-2MM") && s.ScrapQuantity == 5.0m);
    }
}
