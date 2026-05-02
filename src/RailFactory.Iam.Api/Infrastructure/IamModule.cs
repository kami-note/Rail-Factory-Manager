using RailFactory.Iam.Api.Application;
using RailFactory.Iam.Api.Application.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth;
using Npgsql;

namespace RailFactory.Iam.Api.Infrastructure;

public static class IamModule
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, IConfiguration configuration)
    {
        var iamConnectionString = configuration.GetConnectionString("iamdb");
        if (string.IsNullOrWhiteSpace(iamConnectionString))
        {
            services.AddSingleton<IIamLocalUserRepository, InMemoryIamLocalUserRepository>();
        }
        else
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(iamConnectionString));
            services.AddHostedService<IamLocalUsersSchemaInitializer>();
            services.AddScoped<IIamLocalUserRepository, PostgresIamLocalUserRepository>();
        }

        services.AddScoped<GetIamInfo>();
        services.AddScoped<StartExternalLogin>();
        services.AddScoped<FinalizeExternalLogin>();
        services.AddScoped<UpsertLocalUserFromExternalLogin>();
        services.AddScoped<IExternalIdentityProvider, GoogleExternalIdentityProvider>();
        return services;
    }
}
