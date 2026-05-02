using RailFactory.Iam.Api.Application.Auth;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

internal sealed class InMemoryIamLocalUserRepository : IIamLocalUserRepository
{
    private readonly Dictionary<string, IamLocalUser> users = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(IamLocalUser user, CancellationToken cancellationToken)
    {
        var key = $"{user.TenantCode}:{user.ExternalProvider}:{user.ExternalSubject}";
        users[key] = user;
        return Task.CompletedTask;
    }
}
