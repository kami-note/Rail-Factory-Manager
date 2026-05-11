using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using RailFactory.BuildingBlocks.Domain;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// EF Core Interceptor that automatically populates audit fields for IAuditable entities.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IUserContext userContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries<IAuditable>();
        var currentUser = userContext.Email;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedBy).CurrentValue = currentUser;
                entry.Property(x => x.LastModifiedBy).CurrentValue = currentUser;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.LastModifiedBy).CurrentValue = currentUser;
            }
        }
    }
}
