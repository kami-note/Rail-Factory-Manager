using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.Auth;

/// <summary>
/// Retrieves all atomic permissions for a specific user within the current tenant context.
/// Includes Redis caching for high-performance session enrichment.
/// </summary>
public sealed class GetUserPermissions(
    IamAuthDbContext dbContext, 
    IDistributedCache cache,
    ITenantContextAccessor tenantAccessor)
{
    public async Task<IEnumerable<string>> ExecuteAsync(
        string externalProvider,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;
        var cacheKey = $"permissions:{tenantCode}:{externalProvider}:{externalSubject}";

        // Try to get from cache
        var cachedData = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedData))
        {
            return JsonSerializer.Deserialize<List<string>>(cachedData) ?? [];
        }

        // 1. Get IDs of roles directly assigned to the user
        var directRoleIds = await dbContext.UserRoles
            .Where(ur => ur.ExternalProvider == externalProvider && ur.ExternalSubject == externalSubject)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        if (directRoleIds.Count == 0)
        {
            return [];
        }

        // 2. Fetch all roles defined for the tenant to resolve hierarchy in memory
        var allTenantRoles = await dbContext.Roles
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        // 3. Recursive resolution (using a queue to avoid recursion overhead and protect against cycles)
        var resolvedPermissions = new HashSet<string>();
        var visitedRoleIds = new HashSet<Guid>();
        var queue = new Queue<Guid>(directRoleIds);

        while (queue.Count > 0)
        {
            var roleId = queue.Dequeue();
            if (!visitedRoleIds.Add(roleId)) continue; // Already processed this role

            if (allTenantRoles.TryGetValue(roleId, out var role))
            {
                // Collect atomic permissions
                foreach (var permission in role.Permissions)
                {
                    resolvedPermissions.Add(permission);
                }

                // Add children to the queue for traversal
                foreach (var childId in role.ChildRoleIds)
                {
                    queue.Enqueue(childId);
                }
            }
        }

        var permissions = resolvedPermissions.OrderBy(p => p).ToList();

        // Save to cache
        var cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };
        
        await cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(permissions), 
            cacheOptions, 
            cancellationToken);

        return permissions;
    }
}
