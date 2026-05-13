using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

/// <summary>
/// Removes a role assignment from a user.
/// </summary>
public sealed class RemoveRoleFromUser(
    IamAuthDbContext dbContext,
    ITenantContextAccessor tenantAccessor,
    IDistributedCache cache)
{
    public async Task<bool> ExecuteAsync(string email, Guid roleId, CancellationToken cancellationToken)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;

        // Find user
        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null) return false;

        // Find the specific assignment
        var assignment = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur => 
                ur.ExternalProvider == user.ExternalProvider && 
                ur.ExternalSubject == user.ExternalSubject && 
                ur.RoleId == roleId, 
            cancellationToken);

        if (assignment == null) return true; // Already removed

        dbContext.UserRoles.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate Cache
        var cacheKey = $"permissions:{tenantCode}:{user.ExternalProvider}:{user.ExternalSubject}";
        await cache.RemoveAsync(cacheKey, cancellationToken);

        return true;
    }
}
