using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

namespace RailFactory.Tenancy.Api.Tests;

/// <summary>
/// Contains unit/integration tests for the <see cref="PostgresTenantRepository"/> class.
/// </summary>
public class PostgresTenantRepositoryTests : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;
    private readonly TenancyDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTenantRepositoryTests"/> class,
    /// setting up an in-memory SQLite database for testing EF Core database operations.
    /// </summary>
    public PostgresTenantRepositoryTests()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
        using (var cmd = _sqliteConnection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<TenancyDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _dbContext = new TenancyDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Cleans up SQLite in-memory database connections after test execution.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _sqliteConnection.Dispose();
    }

    /// <summary>
    /// Verifies that when a postgres server connection string is configured,
    /// localhost connection strings are dynamically updated to the new host, port, username, and password.
    /// </summary>
    [Fact]
    public async Task FindByCodeAsync_WithConfiguredPostgresServer_RewritesLocalhostConnectionStrings()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config.GetSection("ConnectionStrings")["postgres"]
            .Returns("Host=localhost;Port=55555;Username=new_user;Password=new_password");

        var record = new TenantRecord
        {
            Code = "test-tenant",
            DisplayName = "Test Tenant",
            Locale = "pt-BR",
            TimeZone = "America/Sao_Paulo",
            Status = TenantStatus.Active.ToString(),
            ConnectionStrings = new Dictionary<string, string>
            {
                ["logisticsdb"] = "Host=localhost;Port=33147;Database=tenant-test-logisticsdb;Username=old_user;Password=old_password",
                ["iamdb"] = "Host=127.0.0.1;Port=33147;Database=tenant-test-iamdb;Username=old_user;Password=old_password"
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tenants.Add(record);
        await _dbContext.SaveChangesAsync();

        var repository = new PostgresTenantRepository(_dbContext, config);

        // Act
        var result = await repository.FindByCodeAsync("test-tenant");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("test-tenant");

        result.ConnectionStrings["logisticsdb"].Should().Contain("Host=localhost");
        result.ConnectionStrings["logisticsdb"].Should().Contain("Port=55555");
        result.ConnectionStrings["logisticsdb"].Should().Contain("Database=tenant-test-logisticsdb");
        result.ConnectionStrings["logisticsdb"].Should().Contain("Username=new_user");
        result.ConnectionStrings["logisticsdb"].Should().Contain("Password=new_password");

        result.ConnectionStrings["iamdb"].Should().Contain("Host=localhost"); // Npgsql builder standardizes Host to localhost
        result.ConnectionStrings["iamdb"].Should().Contain("Port=55555");
        result.ConnectionStrings["iamdb"].Should().Contain("Database=tenant-test-iamdb");
        result.ConnectionStrings["iamdb"].Should().Contain("Username=new_user");
        result.ConnectionStrings["iamdb"].Should().Contain("Password=new_password");
    }

    /// <summary>
    /// Verifies that when a postgres server connection string is NOT configured,
    /// tenant connection strings are returned exactly as stored.
    /// </summary>
    [Fact]
    public async Task FindByCodeAsync_WithoutConfiguredPostgresServer_ReturnsOriginalConnectionStrings()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config.GetSection("ConnectionStrings")["postgres"].Returns((string?)null);

        var originalConnectionString = "Host=localhost;Port=33147;Database=tenant-test-logisticsdb;Username=old_user;Password=old_password";
        var record = new TenantRecord
        {
            Code = "test-tenant-2",
            DisplayName = "Test Tenant 2",
            Locale = "pt-BR",
            TimeZone = "America/Sao_Paulo",
            Status = TenantStatus.Active.ToString(),
            ConnectionStrings = new Dictionary<string, string>
            {
                ["logisticsdb"] = originalConnectionString
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tenants.Add(record);
        await _dbContext.SaveChangesAsync();

        var repository = new PostgresTenantRepository(_dbContext, config);

        // Act
        var result = await repository.FindByCodeAsync("test-tenant-2");

        // Assert
        result.Should().NotBeNull();
        result!.ConnectionStrings["logisticsdb"].Should().Be(originalConnectionString);
    }

    /// <summary>
    /// Verifies that when a postgres server connection string is configured,
    /// non-localhost connection strings (e.g. remote servers) are not modified.
    /// </summary>
    [Fact]
    public async Task FindByCodeAsync_WithConfiguredPostgresServer_DoesNotModifyRemoteConnectionStrings()
    {
        // Arrange
        var config = Substitute.For<IConfiguration>();
        config.GetSection("ConnectionStrings")["postgres"]
            .Returns("Host=localhost;Port=55555;Username=new_user;Password=new_password");

        var remoteConnectionString = "Host=remote-postgres-server.com;Port=5432;Database=tenant-test-logisticsdb;Username=some_user;Password=some_password";
        var record = new TenantRecord
        {
            Code = "test-tenant-3",
            DisplayName = "Test Tenant 3",
            Locale = "pt-BR",
            TimeZone = "America/Sao_Paulo",
            Status = TenantStatus.Active.ToString(),
            ConnectionStrings = new Dictionary<string, string>
            {
                ["logisticsdb"] = remoteConnectionString
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Tenants.Add(record);
        await _dbContext.SaveChangesAsync();

        var repository = new PostgresTenantRepository(_dbContext, config);

        // Act
        var result = await repository.FindByCodeAsync("test-tenant-3");

        // Assert
        result.Should().NotBeNull();
        result!.ConnectionStrings["logisticsdb"].Should().Be(remoteConnectionString);
    }
}
