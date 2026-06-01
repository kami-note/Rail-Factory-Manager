using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

namespace RailFactory.Iam.Api.Application.ApiKeys;

/// <summary>
/// Validates a plaintext API key and returns the granted permissions if valid.
/// Used by the Gateway or services to authenticate machine-to-machine calls.
/// </summary>
public sealed class ValidateApiKey(IamAuthDbContext dbContext)
{
    public async Task<ApiKeyValidationResult> ExecuteAsync(string plaintextKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey))
            return ApiKeyValidationResult.Invalid;

        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintextKey)));
        var key = await dbContext.ApiKeys.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash, ct);

        if (key is null || !key.IsActive)
            return ApiKeyValidationResult.Invalid;

        var permissions = JsonSerializer.Deserialize<string[]>(key.PermissionsJson) ?? [];
        return new ApiKeyValidationResult(true, key.TenantCode, permissions);
    }
}

public sealed record ApiKeyValidationResult(bool IsValid, string? TenantCode = null, string[]? Permissions = null)
{
    public static readonly ApiKeyValidationResult Invalid = new(false);
}
