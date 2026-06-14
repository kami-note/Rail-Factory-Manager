using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Tests;

public class LogisticsSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<LogisticsDbContext> _dbContextOptions;

    public LogisticsSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        _dbContextOptions = new DbContextOptionsBuilder<LogisticsDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new LogisticsDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsLogisticsData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new LogisticsDbContext(
            sp.GetRequiredService<DbContextOptions<LogisticsDbContext>>()));
        
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
        var logger = new TestLogger<LogisticsSchemaInitializer>();

        var initializer = new LogisticsSchemaInitializer(serviceProvider, hostEnv, logger);

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
        using var assertContext = new LogisticsDbContext(_dbContextOptions);
        
        var carriers = await assertContext.Carriers.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(carriers);
        Assert.Contains(carriers, c => c.DocumentNumber == "12345678000100" && c.Name == "TransRápido Sorocaba Ltda" && c.WebhookUrl == "https://api.transrapido.com.br/webhook");
        Assert.Contains(carriers, c => c.DocumentNumber == "98765432000111" && c.Name == "Logística Brasil S.A." && c.WebhookUrl == null);

        var orders = await assertContext.ShipmentOrders.Include(o => o.Items).IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(orders);
        var order = orders.FirstOrDefault(o => o.OrderNumber == "EXP-20260610-001");
        Assert.NotNull(order);
        Assert.Equal(ShipmentOrderStatus.Shipped, order.Status);
        Assert.Equal("Metalúrgica Jundiaí", order.RecipientName);
        Assert.Equal("13212000", order.RecipientZipCode);
        Assert.Single(order.Items);
        var item = order.Items.First();
        Assert.Equal("PRO-TR-100", item.MaterialCode);
        Assert.Equal(5m, item.Quantity);
        Assert.Equal(500m, item.WeightKg);
        Assert.Equal(0.5m, item.VolumeCbm);

        var dispatches = await assertContext.Dispatches.IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(dispatches);
        var dispatch = dispatches.FirstOrDefault(d => d.TrackingCode == "RF-TRJ1001");
        Assert.NotNull(dispatch);
        Assert.Equal(order.Id, dispatch.ShipmentOrderId);
        Assert.Equal(DispatchStatus.Delivered, dispatch.Status);
        Assert.Equal(225.00m, dispatch.FreightValueBrl);
        Assert.Equal("BRA2S19", dispatch.VehiclePlate);
        Assert.Equal("Marcos Oliveira", dispatch.DriverName);
        Assert.Equal("Authorized", dispatch.FiscalStatus);
        Assert.Equal("35260612345678000100550010000010011000010012", dispatch.FiscalAccessKey);
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
