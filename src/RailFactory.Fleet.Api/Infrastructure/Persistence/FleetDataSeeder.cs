using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

/// <summary>
/// Seeds initial fleet data for local development.
/// </summary>
public static class FleetDataSeeder
{
    /// <summary>
    /// Seeds default vehicles and driver assignments if environment is Development.
    /// </summary>
    public static async Task SeedAsync(
        FleetDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Create Vehicles
        // Vehicle 1: BRA2S19 (Truck)
        var v1 = await dbContext.Vehicles.Include(v => v.Assignments).IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Plate == "BRA2S19", cancellationToken);
        if (v1 == null)
        {
            v1 = Vehicle.Create("BRA2S19", "9BWZZZ99Z99999999", "123456789", "12345678", VehicleType.Truck, 12000m, 45m, new DateOnly(2027, 12, 31));
            
            // Driver Assignment: Marcos Oliveira (id: 33333333-3333-3333-3333-333333333333)
            var driverId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            v1.AssignDriver(driverId, new DateOnly(2026, 6, 10), null, "Alocação operational padrão.");
            
            dbContext.Vehicles.Add(v1);
        }

        // Vehicle 2: XYZ8765 (Van)
        var v2 = await dbContext.Vehicles.Include(v => v.Assignments).IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Plate == "XYZ8765", cancellationToken);
        if (v2 == null)
        {
            v2 = Vehicle.Create("XYZ8765", "9BWZZZ88Z88888888", "987654321", "87654321", VehicleType.Van, 5000m, 20m, new DateOnly(2027, 12, 31));
            dbContext.Vehicles.Add(v2);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
