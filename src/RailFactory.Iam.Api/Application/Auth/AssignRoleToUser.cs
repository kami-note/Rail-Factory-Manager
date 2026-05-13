using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

/// <summary>
/// Assigns a role to a user identified by their email.
/// </summary>
public sealed class AssignRoleToUser(
    IamAuthDbContext dbContext, 
    ITenantContextAccessor tenantAccessor,
    IDistributedCache cache)
{
    public async Task<bool> ExecuteAsync(string email, Guid roleId, CancellationToken cancellationToken)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;

        // Find the user by email.
        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            return false; // User not found
        }

        // Verify role exists for this tenant (QueryFilter takes care of tenant)
        var roleExists = await dbContext.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
        {
            return false;
        }

        // Check if already assigned
        var alreadyAssigned = await dbContext.UserRoles.AnyAsync(
            ur => ur.ExternalProvider == user.ExternalProvider && 
                  ur.ExternalSubject == user.ExternalSubject && 
                  ur.RoleId == roleId, 
            cancellationToken);

        if (alreadyAssigned)
        {
            return true;
        }

        var assignment = new IamTenantUserRoleRecord
        {
            TenantCode = tenantCode,
            ExternalProvider = user.ExternalProvider,
            ExternalSubject = user.ExternalSubject,
            RoleId = roleId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        dbContext.UserRoles.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Invalidate Cache
        var cacheKey = $"permissions:{tenantCode}:{user.ExternalProvider}:{user.ExternalSubject}";
        await cache.RemoveAsync(cacheKey, cancellationToken);
        
        return true;
    }
}
