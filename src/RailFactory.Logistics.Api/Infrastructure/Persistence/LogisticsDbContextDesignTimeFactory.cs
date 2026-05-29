using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class LogisticsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogisticsDbContext>
{
    public LogisticsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LogisticsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=logisticsdb;Username=postgres;Password=postgres");
        return new LogisticsDbContext(optionsBuilder.Options);
    }
}
