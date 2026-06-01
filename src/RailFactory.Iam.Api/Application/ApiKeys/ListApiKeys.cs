using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Domain;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.ApiKeys;

public sealed class ListApiKeys(IamAuthDbContext dbContext)
{
    public Task<List<IamApiKey>> ExecuteAsync(CancellationToken ct)
        => dbContext.ApiKeys.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
