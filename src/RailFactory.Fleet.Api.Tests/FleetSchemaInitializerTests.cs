using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Fleet.Api.Domain;
using RailFactory.Fleet.Api.Infrastructure.Persistence;

namespace RailFactory.Fleet.Api.Tests;

public class FleetSchemaInitializerTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly DbContextOptions<FleetDbContext> _dbContextOptions;

    public FleetSchemaInitializerTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        _dbContextOptions = new DbContextOptionsBuilder<FleetDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        // Ensure schema is created once on the shared connection
        using var setupContext = new FleetDbContext(_dbContextOptions);
        setupContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_InDevelopment_SeedsFleetData()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_dbContextOptions);
        services.AddScoped(sp => new FleetDbContext(
            sp.GetRequiredService<DbContextOptions<FleetDbContext>>()));
        
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
        var logger = new TestLogger<FleetSchemaInitializer>();

        var initializer = new FleetSchemaInitializer(serviceProvider, hostEnv, logger);

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
        using var assertContext = new FleetDbContext(_dbContextOptions);
        
        var vehicles = await assertContext.Vehicles.Include(v => v.Assignments).IgnoreQueryFilters().ToListAsync();
        Assert.NotEmpty(vehicles);
        
        var v1 = vehicles.FirstOrDefault(v => v.Plate == "BRA2S19");
        Assert.NotNull(v1);
        Assert.Equal("9BWZZZ99Z99999999", v1.Chassis);
        Assert.Equal("123456789", v1.Renavam);
        Assert.Equal("12345678", v1.Rntrc);
        Assert.Equal(VehicleType.Truck, v1.Type);
        Assert.Equal(VehicleStatus.Active, v1.Status);
        Assert.Equal(12000m, v1.MaxWeightKg);
        Assert.Equal(45m, v1.MaxVolumeCbm);
        
        // Check Marcos assignment on BRA2S19
        Assert.Single(v1.Assignments);
        var ass1 = v1.Assignments.First();
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), ass1.DriverPersonId);

        var v2 = vehicles.FirstOrDefault(v => v.Plate == "XYZ8765");
        Assert.NotNull(v2);
        Assert.Equal("9BWZZZ88Z88888888", v2.Chassis);
        Assert.Equal("987654321", v2.Renavam);
        Assert.Equal("87654321", v2.Rntrc);
        Assert.Equal(VehicleType.Van, v2.Type);
        Assert.Equal(VehicleStatus.Active, v2.Status);
        Assert.Equal(5000m, v2.MaxWeightKg);
        Assert.Equal(20m, v2.MaxVolumeCbm);
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
