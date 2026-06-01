using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.ApiKeys;

public sealed class RevokeApiKey(IamAuthDbContext dbContext)
{
    public async Task<bool> ExecuteAsync(Guid keyId, CancellationToken ct)
    {
        var key = await dbContext.ApiKeys.FirstOrDefaultAsync(x => x.Id == keyId, ct);
        if (key is null) return false;

        key.Revoke();
        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}
