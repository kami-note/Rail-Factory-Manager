using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Carriers;
using RailFactory.Logistics.Api.Application.Dispatches;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Application.Shipments;
using RailFactory.Logistics.Api.Infrastructure.Integration;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure;

public static class LogisticsModule
{
    public static IServiceCollection AddLogisticsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LogisticsDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("logisticsdb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<LogisticsSchemaInitializer>();
        services.AddHostedService<LogisticsInventoryDispatcher>();
        services.AddHostedService<LogisticsWebhookDispatcher>();
        services.AddHttpClient("logistics-webhook")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton<RabbitMqPublisher>(sp => new RabbitMqPublisher(
            sp.GetRequiredService<RabbitMQ.Client.IConnection>(),
            IntegrationConstants.Exchanges.Logistics));

        services.AddScoped<ICarrierRepository, PostgresCarrierRepository>();
        services.AddScoped<IShipmentOrderRepository, PostgresShipmentOrderRepository>();
        services.AddScoped<IDispatchRepository, PostgresDispatchRepository>();
        services.AddScoped<ILogisticsOutboxRepository, PostgresLogisticsOutboxRepository>();

        services.AddScoped<CreateCarrier>();
        services.AddScoped<ActivateCarrier>();
        services.AddScoped<DeactivateCarrier>();
        services.AddScoped<ListCarriers>();

        services.AddScoped<CreateShipmentOrder>();
        services.AddScoped<AddShipmentItem>();
        services.AddScoped<StartPicking>();
        services.AddScoped<StartPacking>();
        services.AddScoped<MarkReadyToShip>();
        services.AddScoped<CancelShipmentOrder>();
        services.AddScoped<ListShipmentOrders>();

        services.AddScoped<CreateDispatch>();
        services.AddScoped<ConferenceDispatch>();
        services.AddScoped<ShipDispatch>();
        services.AddScoped<DeliverDispatch>();
        services.AddScoped<GetDispatchByTrackingCode>();

        services.AddHealthChecks()
            .AddCheck("logistics-db-check", () =>
                HealthCheckResult.Healthy("Database connectivity verified at runtime."),
                tags: ["ready"]);

        return services;
    }
}
