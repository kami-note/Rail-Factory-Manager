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
        app.UseAuthentication();
        app.UseTenantResolution();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        return app;
    }
}
