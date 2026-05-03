using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SupplyChainDbContext>
{
    public SupplyChainDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SupplyChainDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=supplychaindb;Username=postgres;Password=postgres");
        return new SupplyChainDbContext(optionsBuilder.Options);
    }
}
