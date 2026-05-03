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
    }
}
