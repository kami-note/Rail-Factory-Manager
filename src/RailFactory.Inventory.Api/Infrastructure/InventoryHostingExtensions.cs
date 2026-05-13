namespace RailFactory.Inventory.Api.Infrastructure;

public static class InventoryHostingExtensions
{
    public static WebApplicationBuilder AddInventoryHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddTenantResolution();
        return builder;
    }

    public static WebApplication UseInventoryHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseAuthentication();
        app.UseTenantResolution();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        return app;
    }
}
