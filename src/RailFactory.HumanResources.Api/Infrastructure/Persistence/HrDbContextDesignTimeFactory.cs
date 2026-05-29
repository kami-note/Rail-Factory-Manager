using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class HrDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HrDbContext>
{
    public HrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HrDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=hrdb;Username=postgres;Password=postgres");
        return new HrDbContext(optionsBuilder.Options);
    }
}
