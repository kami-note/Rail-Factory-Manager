using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamAuthDbContextDesignTimeFactory : IDesignTimeDbContextFactory<IamAuthDbContext>
{
    public IamAuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IamAuthDbContext>();
        var connectionString =
            "Host=localhost;Port=5432;Database=iamdb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new IamAuthDbContext(optionsBuilder.Options);
    }
}
