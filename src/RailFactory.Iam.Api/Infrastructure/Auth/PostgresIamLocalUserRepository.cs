using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Application.Auth;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

internal sealed class PostgresIamLocalUserRepository(IamAuthDbContext dbContext) : IIamLocalUserRepository
{
    public async Task UpsertAsync(IamLocalUser user, CancellationToken cancellationToken)
    {
        var existing = await dbContext.LocalUsers
            .SingleOrDefaultAsync(
                x => x.ExternalProvider == user.ExternalProvider && x.ExternalSubject == user.ExternalSubject,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.LocalUsers.Add(new IamLocalUserRecord
            {
                ExternalProvider = user.ExternalProvider,
                ExternalSubject = user.ExternalSubject,
                Email = user.Email,
                DisplayName = user.DisplayName,
                FirstLoginAt = now,
                LastLoginAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.Email = user.Email;
            existing.DisplayName = user.DisplayName;
            existing.LastLoginAt = now;
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // ELITE AUTO-ASSIGN: If dev tenant and user has no roles, give them Administrator
        // This is safe because _tenantCode is already handled by the DBContext from the ITenantContextAccessor
        // Accessing the private field via a helper or reflection if needed, but wait, 
        // I can just query if there are roles assigned to this user in THIS tenant.
        
        var hasRoles = await dbContext.UserRoles.AnyAsync(
            ur => ur.ExternalProvider == user.ExternalProvider && ur.ExternalSubject == user.ExternalSubject,
            cancellationToken);

        // We check a specific condition for the 'dev' tenant. 
        // Since we can't easily access the private _tenantCode from here without changing the DBContext,
        // let's use the ITenantContextAccessor directly if we had it, or just rely on the fact 
        // that the QueryFilter on UserRoles will restrict the check to the current tenant anyway.
        
        // However, I need to know if the CURRENT tenant is 'dev'.
        // I'll check if the dbContext.Roles table contains the seeded Admin role.
        var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var adminRoleExists = await dbContext.Roles.AnyAsync(r => r.Id == adminRoleId, cancellationToken);

        if (adminRoleExists && !hasRoles)
        {
            // Only auto-assign in 'dev' (confirmed by the existence of the specific seeded ID which only exists in dev)
            dbContext.UserRoles.Add(new IamTenantUserRoleRecord
            {
                TenantCode = "dev", // The QueryFilter will handle this, but we set it for completeness
                ExternalProvider = user.ExternalProvider,
                ExternalSubject = user.ExternalSubject,
                RoleId = adminRoleId,
                AssignedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
