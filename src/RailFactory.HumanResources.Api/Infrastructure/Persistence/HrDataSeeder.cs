using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

/// <summary>
/// Seeds initial human resources data for local development.
/// </summary>
public static class HrDataSeeder
{
    /// <summary>
    /// Seeds default employees and drivers if environment is Development.
    /// </summary>
    public static async Task SeedAsync(
        HrDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Carlos Silva (Employee)
        var carlos = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "01234567890", cancellationToken);
        if (carlos == null)
        {
            carlos = Person.Create("Carlos Silva", "01234567890", PersonType.Employee, "carlos@railfactory.com.br", id: Guid.Parse("11111111-1111-1111-1111-111111111111"));
            dbContext.People.Add(carlos);
        }

        // 2. Ana Souza (Employee)
        var ana = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "98765432100", cancellationToken);
        if (ana == null)
        {
            ana = Person.Create("Ana Souza", "98765432100", PersonType.Employee, "ana@railfactory.com.br", id: Guid.Parse("22222222-2222-2222-2222-222222222222"));
            dbContext.People.Add(ana);
        }

        // 3. Marcos Oliveira (Driver)
        var marcos = await dbContext.People.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.DocumentNumber == "45678912300", cancellationToken);
        if (marcos == null)
        {
            marcos = Person.Create("Marcos Oliveira", "45678912300", PersonType.Driver, "marcos@railfactory.com.br", id: Guid.Parse("33333333-3333-3333-3333-333333333333"));
            dbContext.People.Add(marcos);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
