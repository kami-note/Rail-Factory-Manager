using System.Text.Json;
using RailFactory.Iam.Api.Domain;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.ApiKeys;

public sealed class GenerateApiKey(IamAuthDbContext dbContext, ITenantContextAccessor tenantAccessor)
{
    public async Task<(IamApiKey Entity, string PlaintextKey)> ExecuteAsync(
        GenerateApiKeyInput input, CancellationToken ct)
    {
        var tenantCode = tenantAccessor.Current!.TenantCode;
        var permissionsJson = JsonSerializer.Serialize(input.Permissions ?? []);
        var (entity, plaintextKey) = IamApiKey.Generate(tenantCode, input.Name, input.CreatedByEmail, permissionsJson, input.ExpiresAt);
        dbContext.ApiKeys.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return (entity, plaintextKey);
    }
}

public sealed record GenerateApiKeyInput(
    string Name,
    string CreatedByEmail,
    string[]? Permissions,
    DateTimeOffset? ExpiresAt);
