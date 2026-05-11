using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RailFactory.BuildingBlocks.Tenancy;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Implementation of IUserContext that retrieves identity from the HTTP Claims.
/// </summary>
internal sealed class UserContextAccessor(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private const string DefaultSystemEmail = "system@railfactory.com";
    private string? _systemIdentity;

    public EmailAddress Email
    {
        get
        {
            var email = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrWhiteSpace(email))
            {
                // Fallback for non-interactive or unauthenticated contexts
                return EmailAddress.From(_systemIdentity ?? DefaultSystemEmail);
            }

            return EmailAddress.From(email);
        }
    }

    public void SetSystemIdentity(string identityName)
    {
        _systemIdentity = identityName.Contains('@') ? identityName : $"{identityName}@railfactory.com";
    }
}
