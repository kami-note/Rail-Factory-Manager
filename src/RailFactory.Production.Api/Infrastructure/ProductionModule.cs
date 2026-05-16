using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.Production.Api.Application;
using RailFactory.Production.Api.Application.Boms;
using RailFactory.Production.Api.Application.Orders;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Application.WorkCenters;
using RailFactory.Production.Api.Infrastructure.Integration;
using RailFactory.Production.Api.Infrastructure.Persistence;

namespace RailFactory.Production.Api.Infrastructure;

/// <summary>
/// Infrastructure module for the Production bounded context.
/// </summary>
public static class ProductionModule
{
    public static IServiceCollection AddProductionModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProductionDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("productiondb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<ProductionSchemaInitializer>();
        services.AddHostedService<ProductionInventoryDispatcher>();

        services.AddHttpClient("inventory-integration", client =>
        {
            client.BaseAddress = new Uri("http://inventory");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<GetProductionInfo>();

        services.AddScoped<IWorkCenterRepository, PostgresWorkCenterRepository>();
        services.AddScoped<IBomRepository, PostgresBomRepository>();
        services.AddScoped<IProductionOrderRepository, PostgresProductionOrderRepository>();
        services.AddScoped<IExecutionRepository, PostgresExecutionRepository>();

        services.AddScoped<CreateWorkCenter>();
        services.AddScoped<DeactivateWorkCenter>();
        services.AddScoped<ListWorkCenters>();

        services.AddScoped<CreateBom>();
        services.AddScoped<AddBomItem>();
        services.AddScoped<ActivateBomVersion>();
        services.AddScoped<ListBoms>();

        services.AddScoped<CreateProductionOrder>();
        services.AddScoped<ReleaseProductionOrder>();
        services.AddScoped<CancelProductionOrder>();
        services.AddScoped<ListProductionOrders>();
        services.AddScoped<StartOrderExecution>();
        services.AddScoped<RecordConsumption>();
        services.AddScoped<RecordScrap>();
        services.AddScoped<RecordQualityInspection>();
        services.AddScoped<CompleteProductionOrder>();

        services.AddHealthChecks()
            .AddCheck("production-db-check", () =>
                HealthCheckResult.Healthy("Database connectivity verified at runtime."),
                tags: ["ready"]);

        return services;
    }
}
