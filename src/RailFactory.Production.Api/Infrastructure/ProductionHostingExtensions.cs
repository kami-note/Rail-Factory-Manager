namespace RailFactory.Production.Api.Infrastructure;

public static class ProductionHostingExtensions
{
    public static WebApplicationBuilder AddProductionHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.AddTenantResolution();
        builder.AddRabbitMQClient("rabbitmq");
        return builder;
    }

    public static WebApplication UseProductionHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseAuthentication();
        app.UseTenantResolution();
        app.UseInternalTokenTenantBinding();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        return app;
    }
}
