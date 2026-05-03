using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class TenancyDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    public TenancyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenancyDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tenantcatalog;Username=postgres;Password=postgres");
        return new TenancyDbContext(optionsBuilder.Options);
    }
}
