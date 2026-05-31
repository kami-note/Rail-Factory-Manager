using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Api.Domain;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

public sealed class AssignRoleToUser(
    IamAuthDbContext dbContext,
    ITenantContextAccessor tenantAccessor,
    IDistributedCache cache,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<bool> ExecuteAsync(string email, Guid roleId, CancellationToken cancellationToken)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;

        var user = await dbContext.LocalUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null) return false;

        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role == null) return false;

        var alreadyAssigned = await dbContext.UserRoles.AnyAsync(
            ur => ur.ExternalProvider == user.ExternalProvider &&
                  ur.ExternalSubject == user.ExternalSubject &&
                  ur.RoleId == roleId,
            cancellationToken);

        if (alreadyAssigned) return true;

        var assignment = new IamTenantUserRoleRecord
        {
            TenantCode = tenantCode,
            ExternalProvider = user.ExternalProvider,
            ExternalSubject = user.ExternalSubject,
            RoleId = roleId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        dbContext.UserRoles.Add(assignment);

        // Audit policy: role_assigned is FAIL-CLOSED — role change and audit are saved in the same
        // EF transaction via SaveChangesAsync. If the audit write fails, the role is not granted.
        var ctx = httpContextAccessor.HttpContext;
        var actorEmail = ctx?.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "system@railfactory.com";
        var metadata = JsonSerializer.Serialize(new { roleId = role.Id, roleName = role.Name, tenantCode });
        dbContext.AuditEntries.Add(IamAuditEntry.Create(
            "role_assigned", actorEmail, email,
            IamRequestContext.ExtractIpAddress(ctx),
            IamRequestContext.ExtractCorrelationId(ctx),
            metadata));

        await dbContext.SaveChangesAsync(cancellationToken);

        var cacheKey = $"permissions:{tenantCode}:{user.ExternalProvider}:{user.ExternalSubject}";
        await cache.RemoveAsync(cacheKey, cancellationToken);

        return true;
    }
}
