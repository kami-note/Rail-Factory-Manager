using RailFactory.Production.Api.Application;

namespace RailFactory.Production.Api.Infrastructure;

public static class ProductionModule
{
    public static IServiceCollection AddProductionModule(this IServiceCollection services)
    {
        services.AddScoped<GetProductionInfo>();
        return services;
    }
}
