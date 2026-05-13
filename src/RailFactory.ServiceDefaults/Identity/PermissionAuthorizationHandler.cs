using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Requirement that ensures the user has a specific atomic permission.
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// Handler that validates the 'permission' claim against the requirement.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims
            .Any(c => c.Type == "permission" && c.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
