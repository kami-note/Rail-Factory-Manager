using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

public sealed record TenantUserResponse(string Email, string? Name, List<TenantUserRoleResponse> Roles);
public sealed record TenantUserRoleResponse(Guid RoleId, string RoleName);

/// <summary>
/// Lists all users that have at least one role assigned in the current tenant.
/// </summary>
public sealed class ListTenantUsers(IamAuthDbContext dbContext)
{
    public async Task<IEnumerable<TenantUserResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        // Join UserRoles with Roles and LocalUsers
        var userRoles = await dbContext.UserRoles
            .Include(ur => ur.Role)
            .Join(dbContext.LocalUsers,
                ur => new { ur.ExternalProvider, ur.ExternalSubject },
                u => new { u.ExternalProvider, u.ExternalSubject },
                (ur, u) => new { u.Email, u.DisplayName, ur.RoleId, RoleName = ur.Role!.Name })
            .ToListAsync(cancellationToken);

        // Group by user email
        return userRoles
            .GroupBy(x => new { x.Email, x.DisplayName })
            .Select(g => new TenantUserResponse(
                g.Key.Email ?? "unknown",
                g.Key.DisplayName,
                g.Select(r => new TenantUserRoleResponse(r.RoleId, r.RoleName)).ToList()
            ))
            .OrderBy(u => u.Email)
            .ToList();
    }
}
