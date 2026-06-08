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
        var isNewUser = existing is null;

        if (isNewUser)
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
            existing!.Email = user.Email;
            existing.DisplayName = user.DisplayName;
            existing.LastLoginAt = now;
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // First user to log into this tenant automatically receives the Administrator role.
        // This solves the bootstrap problem: after setup there's no admin to assign roles.
        if (!isNewUser) return;

        var anyRolesAssigned = await dbContext.UserRoles.AnyAsync(cancellationToken);
        if (anyRolesAssigned) return;

        var adminRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == "Administrador do Sistema", cancellationToken);
        if (adminRole is null) return;

        dbContext.UserRoles.Add(new IamTenantUserRoleRecord
        {
            TenantCode = adminRole.TenantCode,
            ExternalProvider = user.ExternalProvider,
            ExternalSubject = user.ExternalSubject,
            RoleId = adminRole.Id,
            AssignedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
