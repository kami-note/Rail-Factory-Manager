using Npgsql;
using RailFactory.Tenancy.Api.Application;

namespace RailFactory.Tenancy.Api.Infrastructure;

public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tenantCatalogConnectionString = configuration.GetConnectionString("tenantcatalog");
        if (string.IsNullOrWhiteSpace(tenantCatalogConnectionString))
        {
            services.AddSingleton<ITenantRepository, InMemoryTenantRepository>();
        }
        else
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(tenantCatalogConnectionString));
            services.AddHostedService<TenantCatalogSchemaInitializer>();
            services.AddScoped<ITenantRepository, PostgresTenantRepository>();
        }

        services.AddScoped<GetTenantByCode>();

        return services;
    }
}
