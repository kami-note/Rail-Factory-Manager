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
        var iamConnectionString = ResolveIamConnectionString(configuration)
            ?? throw new InvalidOperationException(
                "IAM database connection string is required. Configure ConnectionStrings:iamdb or ConnectionStrings:tenant-dev-iamdb.");

        services.AddDbContext<IamAuthDbContext>(options => options.UseNpgsql(iamConnectionString));
        services.AddHostedService<IamLocalUsersSchemaInitializer>();
        services.AddScoped<IIamLocalUserRepository, PostgresIamLocalUserRepository>();

        services.AddScoped<GetIamInfo>();
        services.AddScoped<StartExternalLogin>();
        services.AddScoped<FinalizeExternalLogin>();
        services.AddScoped<UpsertLocalUserFromExternalLogin>();
        services.AddScoped<IExternalIdentityProvider, GoogleExternalIdentityProvider>();
        return services;
    }

    private static string? ResolveIamConnectionString(IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("iamdb");
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return configuration.GetConnectionString("tenant-dev-iamdb");
    }
}
