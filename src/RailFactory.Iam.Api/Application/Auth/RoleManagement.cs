using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;
using RailFactory.Iam.Api.Api.Requests;

namespace RailFactory.Iam.Api.Application.Auth;

/// <summary>
/// Lists all roles defined for the current tenant.
/// </summary>
public sealed class ListTenantRoles(IamAuthDbContext dbContext)
{
    public async Task<IEnumerable<RoleResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description, r.Permissions, r.ChildRoleIds))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Creates a new custom role for the current tenant.
/// </summary>
public sealed class CreateTenantRole(IamAuthDbContext dbContext, ITenantContextAccessor tenantAccessor)
{
    public async Task<Guid> ExecuteAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;
        
        var role = new IamTenantRoleRecord
        {
            Id = Guid.NewGuid(),
            TenantCode = tenantCode,
            Name = request.Name,
            Description = request.Description,
            Permissions = request.Permissions,
            ChildRoleIds = request.ChildRoleIds,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return role.Id;
    }
}
