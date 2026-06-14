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
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

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
        var logger = new TestLogger<SupplyChainSchemaInitializer>();

        var initializer = new SupplyChainSchemaInitializer(serviceProvider, hostEnv, logger);

        // Act
        var cts = new CancellationTokenSource();
        await initializer.StartAsync(cts.Token);

        // Wait deterministically for the migration/seeding to complete
        var waitTask = logger.CompletedTask;
        var timeoutTask = Task.Delay(15000);
        var completedTask = await Task.WhenAny(waitTask, timeoutTask);
        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Database migration/seeding did not complete within 15 seconds.");
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

    private class TestLogger<T> : ILogger<T>
    {
        private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CompletedTask => _tcs.Task;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.Contains("migrated", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("Failed", StringComparison.OrdinalIgnoreCase))
            {
                _tcs.TrySetResult();
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
