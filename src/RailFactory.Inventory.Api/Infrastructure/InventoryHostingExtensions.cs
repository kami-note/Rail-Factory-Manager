namespace RailFactory.Inventory.Api.Infrastructure;

public static class InventoryHostingExtensions
{
    public static WebApplicationBuilder AddInventoryHosting(this WebApplicationBuilder builder)
    {
        ValidateInternalApiKey(builder.Configuration);
        builder.AddServiceDefaults();
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.AddTenantResolution();
        builder.AddRabbitMQClient("rabbitmq");
        return builder;
    }

    public static WebApplication UseInventoryHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseAuthentication();
        app.UseTenantResolution();
        app.UseInternalTokenTenantBinding();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        return app;
    }

    private static void ValidateInternalApiKey(IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["InternalApiKey"]))
        {
            throw new InvalidOperationException("InternalApiKey must be configured.");
        }
    }
}
