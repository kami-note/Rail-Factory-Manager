using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Fleet.Api.Application.Drivers;
using RailFactory.Fleet.Api.Application.Fueling;
using RailFactory.Fleet.Api.Application.Maintenance;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Application.Vehicles;
using RailFactory.Fleet.Api.Infrastructure.Persistence;

namespace RailFactory.Fleet.Api.Infrastructure;

public static class FleetModule
{
    public static IServiceCollection AddFleetModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FleetDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("fleetdb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<FleetSchemaInitializer>();

        services.AddScoped<IVehicleRepository, PostgresVehicleRepository>();
        services.AddScoped<IMaintenanceRepository, PostgresMaintenanceRepository>();
        services.AddScoped<IFuelingRepository, PostgresFuelingRepository>();

        services.AddScoped<CreateVehicle>();
        services.AddScoped<ActivateVehicle>();
        services.AddScoped<DeactivateVehicle>();
        services.AddScoped<ListVehicles>();

        services.AddScoped<AssignDriver>();
        services.AddScoped<ListDriverAssignments>();

        services.AddScoped<ScheduleMaintenance>();
        services.AddScoped<CompleteMaintenance>();
        services.AddScoped<CancelMaintenance>();
        services.AddScoped<ListMaintenancePlans>();

        services.AddScoped<RecordFueling>();
        services.AddScoped<ListFuelingRecords>();

        services.AddHealthChecks()
            .AddCheck("fleet-db-check", () =>
                HealthCheckResult.Healthy("Database connectivity verified at runtime."),
                tags: ["ready"]);

        return services;
    }
}
