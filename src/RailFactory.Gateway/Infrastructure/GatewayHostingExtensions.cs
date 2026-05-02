namespace RailFactory.Gateway.Infrastructure;

public static class GatewayHostingExtensions
{
    public static WebApplicationBuilder AddGatewayHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();
        return builder;
    }

    public static WebApplication UseGatewayHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.MapDefaultEndpoints();
        return app;
    }
}
