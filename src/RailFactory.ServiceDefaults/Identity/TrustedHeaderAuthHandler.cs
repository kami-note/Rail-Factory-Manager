using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Authentication handler that trusts identity information provided in specific HTTP headers.
/// This is used by internal microservices that are behind a trusted Gateway/BFF.
/// </summary>
internal sealed class TrustedHeaderAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var email = Request.Headers[TenantConstants.UserEmailHeaderName].FirstOrDefault();
        var name = Request.Headers[TenantConstants.UserNameHeaderName].FirstOrDefault();
        var permissionsHeader = Request.Headers[TenantConstants.UserPermissionsHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new("sub", email) // Use email as subject for simplicity
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new(ClaimTypes.Name, name));
        }

        if (!string.IsNullOrWhiteSpace(permissionsHeader))
        {
            var permissions = permissionsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var permission in permissions)
            {
                claims.Add(new("permission", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
