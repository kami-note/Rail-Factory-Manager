using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Api.Domain;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

public sealed class RemoveRoleFromUser(
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

        var assignment = await dbContext.UserRoles
            .FirstOrDefaultAsync(ur =>
                ur.ExternalProvider == user.ExternalProvider &&
                ur.ExternalSubject == user.ExternalSubject &&
                ur.RoleId == roleId,
            cancellationToken);

        if (assignment == null) return true;

        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        dbContext.UserRoles.Remove(assignment);

        // Audit policy: role_revoked is FAIL-CLOSED — role removal and audit are saved in the same
        // EF transaction via SaveChangesAsync. If the audit write fails, the role is not revoked.
        var ctx = httpContextAccessor.HttpContext;
        var actorEmail = ctx?.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "system@railfactory.com";
        var metadata = JsonSerializer.Serialize(new { roleId, roleName = role?.Name, tenantCode });
        dbContext.AuditEntries.Add(IamAuditEntry.Create(
            "role_revoked", actorEmail, email,
            IamRequestContext.ExtractIpAddress(ctx),
            IamRequestContext.ExtractCorrelationId(ctx),
            metadata));

        await dbContext.SaveChangesAsync(cancellationToken);

        var cacheKey = $"permissions:{tenantCode}:{user.ExternalProvider}:{user.ExternalSubject}";
        await cache.RemoveAsync(cacheKey, cancellationToken);

        return true;
    }
}
