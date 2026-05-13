using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers the RBAC permission-based authorization infrastructure.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        
        // Elite Fix: Register the TrustedHeader authentication scheme.
        // This allows internal microservices to trust identity headers injected by the Gateway/BFF.
        services.AddAuthentication("TrustedHeaders")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TrustedHeaderAuthHandler>(
                "TrustedHeaders", 
                _ => { });

        return services;
    }

    /// <summary>
    /// Secures an endpoint by requiring a specific atomic permission.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="permission">The permission code (e.g., "inventory.read").</param>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission) 
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy.Requirements.Add(new PermissionRequirement(permission)));
    }
}
