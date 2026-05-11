namespace RailFactory.SupplyChain.Api.Infrastructure;

public static class SupplyChainHostingExtensions
{
    public static WebApplicationBuilder AddSupplyChainHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddTenantResolution();
        return builder;
    }

    public static WebApplication UseSupplyChainHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseTenantResolution();
        app.UseRailFactoryHeaderIdentity(); // ELITE FIX: Enable trusted identity from headers
        app.MapDefaultEndpoints();
        return app;
    }
}
