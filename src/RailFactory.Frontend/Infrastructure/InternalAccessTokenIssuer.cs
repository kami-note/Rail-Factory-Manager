using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RailFactory.BuildingBlocks.Auth;

namespace RailFactory.Frontend.Infrastructure;

internal sealed class InternalAccessTokenIssuer(IOptions<InternalServiceTokenOptions> options)
{
    private readonly InternalServiceTokenOptions _options = options.Value;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public string Issue(AuthSessionDto session, string tenantCode)
    {
        if (session.User?.Email is null)
        {
            throw new InvalidOperationException("Authenticated session must contain a user email to issue an internal token.");
        }

        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("InternalToken:SigningKey must be configured.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, session.User.Email),
            new(ClaimTypes.Email, session.User.Email),
            new(InternalServiceTokenClaimTypes.Tenant, tenantCode)
        };

        if (!string.IsNullOrWhiteSpace(session.User.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, session.User.Name));
        }

        foreach (var permission in session.User.Permissions.Distinct(StringComparer.Ordinal))
        {
            claims.Add(new Claim(InternalServiceTokenClaimTypes.Permission, permission));
        }

        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.LifetimeMinutes <= 0 ? 5 : _options.LifetimeMinutes),
            signingCredentials: credentials);

        return _tokenHandler.WriteToken(token);
    }
}
