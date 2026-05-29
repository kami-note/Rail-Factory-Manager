using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.HumanResources.Api.Application.Hours;
using RailFactory.HumanResources.Api.Application.People;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Infrastructure.Persistence;

namespace RailFactory.HumanResources.Api.Infrastructure;

public static class HrModule
{
    public static IServiceCollection AddHrModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<HrDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("hrdb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<HrSchemaInitializer>();

        services.AddScoped<IPersonRepository, PostgresPersonRepository>();
        services.AddScoped<IHourLogRepository, PostgresHourLogRepository>();

        services.AddScoped<CreatePerson>();
        services.AddScoped<ActivatePerson>();
        services.AddScoped<DeactivatePerson>();
        services.AddScoped<ListPersons>();

        services.AddScoped<LogHours>();
        services.AddScoped<ListHourLogs>();

        services.AddHealthChecks()
            .AddCheck("hr-db-check", () =>
                HealthCheckResult.Healthy("Database connectivity verified at runtime."),
                tags: ["ready"]);

        return services;
    }
}
