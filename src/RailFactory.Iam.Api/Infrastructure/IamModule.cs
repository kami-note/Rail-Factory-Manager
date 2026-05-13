using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Application;
using RailFactory.Iam.Api.Application.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Infrastructure;

public static class IamModule
{
    public static IServiceCollection AddIamModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IamAuthDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("iamdb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<IamLocalUsersSchemaInitializer>();
        services.AddScoped<IIamLocalUserRepository, PostgresIamLocalUserRepository>();

        services.AddScoped<GetIamInfo>();
        services.AddScoped<StartExternalLogin>();
        services.AddScoped<FinalizeExternalLogin>();
        services.AddScoped<UpsertLocalUserFromExternalLogin>();
        services.AddScoped<GetUserPermissions>();
        services.AddScoped<ListTenantRoles>();
        services.AddScoped<CreateTenantRole>();
        services.AddScoped<ListTenantUsers>();
        services.AddScoped<AssignRoleToUser>();
        services.AddScoped<RemoveRoleFromUser>();
        services.AddScoped<IExternalIdentityProvider, GoogleExternalIdentityProvider>();
        return services;
    }
}
