namespace RailFactory.Fleet.Api.Infrastructure;

public static class FleetHostingExtensions
{
    public static WebApplicationBuilder AddFleetHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.AddTenantResolution();
        return builder;
    }

    public static WebApplication UseFleetHosting(this WebApplication app)
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
