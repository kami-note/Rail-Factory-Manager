using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RailFactory.BuildingBlocks.Auth;

namespace Microsoft.Extensions.Hosting;

public static class InternalServiceTokenAuthenticationExtensions
{
    public const string Scheme = JwtBearerDefaults.AuthenticationScheme;

    public static IServiceCollection AddInternalTokenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(Scheme)
            .AddInternalTokenAuthentication(configuration);

        return services;
    }

    public static AuthenticationBuilder AddInternalTokenAuthentication(this AuthenticationBuilder builder, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(InternalServiceTokenOptions.SectionName)
            .Get<InternalServiceTokenOptions>() ?? new InternalServiceTokenOptions();
        ValidateOptions(options);

        builder.AddJwtBearer(Scheme, jwt =>
        {
            jwt.RequireHttpsMetadata = false;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CreateSigningKey(options.SigningKey),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        return builder;
    }

    private static void ValidateOptions(InternalServiceTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("InternalToken:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("InternalToken:Audience must be configured.");
        }

        if (options.LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("InternalToken:LifetimeMinutes must be greater than zero.");
        }
    }

    private static SecurityKey CreateSigningKey(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("InternalToken:SigningKey must be configured.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }
}
