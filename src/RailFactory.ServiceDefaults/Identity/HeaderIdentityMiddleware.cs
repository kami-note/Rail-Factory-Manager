using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Middleware that populates the HttpContext.User from trusted identity headers.
/// This should only be used in internal microservices that trust the Gateway/BFF.
/// </summary>
internal sealed class HeaderIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var email = context.Request.Headers[TenantConstants.UserEmailHeaderName].FirstOrDefault();
        var name = context.Request.Headers[TenantConstants.UserNameHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new("sub", email) // Use email as subject for simplicity in this flow
            };

            if (!string.IsNullOrWhiteSpace(name))
            {
                claims.Add(new(ClaimTypes.Name, name));
            }

            var identity = new ClaimsIdentity(claims, "HeaderTrustedIdentity");
            context.User = new ClaimsPrincipal(identity);
        }

        await next(context);
    }
}
