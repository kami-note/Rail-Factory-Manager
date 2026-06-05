using RailFactory.SupplyChain.Api.Api.ExceptionHandling;

namespace RailFactory.SupplyChain.Api.Infrastructure;

public static class SupplyChainHostingExtensions
{
    public static WebApplicationBuilder AddSupplyChainHosting(this WebApplicationBuilder builder)
    {
        ValidateInternalApiKey(builder.Configuration);
        builder.Services.AddExceptionHandler<GlobalDomainExceptionHandler>();
        builder.AddServiceDefaults();
        builder.Services.AddInternalTokenAuthentication(builder.Configuration);
        builder.AddTenantResolution();
        builder.AddRabbitMQClient("rabbitmq");
        return builder;
    }

    public static WebApplication UseSupplyChainHosting(this WebApplication app)
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
