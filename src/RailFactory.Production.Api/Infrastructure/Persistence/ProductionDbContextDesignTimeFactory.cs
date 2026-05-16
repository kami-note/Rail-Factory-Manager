using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class ProductionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ProductionDbContext>
{
    public ProductionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductionDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=productiondb;Username=postgres;Password=postgres");
        return new ProductionDbContext(optionsBuilder.Options);
    }
}
