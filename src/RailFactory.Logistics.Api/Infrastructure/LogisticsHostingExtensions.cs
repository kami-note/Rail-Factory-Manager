namespace RailFactory.Logistics.Api.Infrastructure;

public static class LogisticsHostingExtensions
{
    public static WebApplicationBuilder AddLogisticsHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.AddTenantResolution();
        builder.AddRabbitMQClient("rabbitmq");
        return builder;
    }

    public static WebApplication UseLogisticsHosting(this WebApplication app)
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
