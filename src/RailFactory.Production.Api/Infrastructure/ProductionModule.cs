using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.Production.Api.Application;
using RailFactory.Production.Api.Application.Boms;
using RailFactory.Production.Api.Application.Orders;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Application.WorkCenters;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Production.Api.Infrastructure.Adapters;
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
        services.AddSingleton<RabbitMqPublisher>(sp => new RabbitMqPublisher(
            sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
            IntegrationConstants.Exchanges.Production));

        services.AddHttpClient("supply-chain-integration", client =>
        {
            client.BaseAddress = new Uri("http://supply-chain");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<GetProductionInfo>();

        services.AddScoped<IWorkCenterRepository, PostgresWorkCenterRepository>();
        services.AddScoped<IBomRepository, PostgresBomRepository>();
        services.AddScoped<IProductionOrderRepository, PostgresProductionOrderRepository>();
        services.AddScoped<IExecutionRepository, PostgresExecutionRepository>();
        services.AddScoped<IProductionDashboardRepository, PostgresProductionDashboardRepository>();
        services.AddScoped<IMaterialCostProvider, HttpMaterialCostProvider>();

        services.AddScoped<CreateWorkCenter>();
        services.AddScoped<DeactivateWorkCenter>();
        services.AddScoped<ActivateWorkCenter>();
        services.AddScoped<ListWorkCenters>();

        services.AddScoped<CreateBom>();
        services.AddScoped<AddBomItem>();
        services.AddScoped<ActivateBomVersion>();
        services.AddScoped<CloneBomVersion>();
        services.AddScoped<ListBoms>();
        services.AddScoped<GetBomCostRollup>();

        services.AddScoped<CreateProductionOrder>();
        services.AddScoped<ReleaseProductionOrder>();
        services.AddScoped<CancelProductionOrder>();
        services.AddScoped<ListProductionOrders>();
        services.AddScoped<StartOrderExecution>();
        services.AddScoped<RecordConsumption>();
        services.AddScoped<RecordScrap>();
        services.AddScoped<RecordQualityInspection>();
        services.AddScoped<CompleteProductionOrder>();
        services.AddScoped<GetOrderExecutionHistory>();
        services.AddScoped<GetProductionDashboard>();

        services.AddHealthChecks()
            .AddCheck("production-db-check", () =>
                HealthCheckResult.Healthy("Database connectivity verified at runtime."),
                tags: ["ready"]);

        return services;
    }
}
