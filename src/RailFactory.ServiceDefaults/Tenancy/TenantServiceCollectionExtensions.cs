using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

internal static class TenantServiceCollectionExtensions
{
    public static TBuilder AddRailFactoryTenantResolution<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContext, UserContextAccessor>();

        builder.Services.AddMemoryCache();
        builder.Services.Configure<TenantRoutingOptions>(builder.Configuration.GetSection("TenantRouting"));
        builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
        builder.Services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        builder.Services.AddHttpClient<ITenantCatalogClient, TenantCatalogHttpClient>(client =>
        {
            client.BaseAddress = new Uri("http://tenant-management");
        })
        .AddStandardResilienceHandler();

        return builder;
    }
}
