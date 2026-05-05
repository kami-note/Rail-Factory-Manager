using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;

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
            throw new InvalidOperationException("Tenant catalog connection string is required.");
        }

        services.AddDbContext<TenancyDbContext>(options => options.UseNpgsql(tenantCatalogConnectionString));
        services.AddHostedService<TenantCatalogSchemaInitializer>();
        services.AddScoped<ITenantRepository, PostgresTenantRepository>();

        services.AddScoped<GetTenantByCode>();
        services.AddScoped<ListTenants>();

        return services;
    }
}
