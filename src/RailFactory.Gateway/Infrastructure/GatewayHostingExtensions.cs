namespace RailFactory.Gateway.Infrastructure;

public static class GatewayHostingExtensions
{
    public static WebApplicationBuilder AddGatewayHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddTenantResolution(); // ELITE FIX: Enable tenant validation at the entry point
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();
        return builder;
    }

    public static WebApplication UseGatewayHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseTenantResolution(); // ELITE FIX: Validate tenant existence before proxying
        app.MapDefaultEndpoints();
        return app;
    }
}
