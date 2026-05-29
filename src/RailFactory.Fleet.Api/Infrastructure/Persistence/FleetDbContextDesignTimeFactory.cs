using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class FleetDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FleetDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=fleetdb;Username=postgres;Password=postgres");
        return new FleetDbContext(optionsBuilder.Options);
    }
}
