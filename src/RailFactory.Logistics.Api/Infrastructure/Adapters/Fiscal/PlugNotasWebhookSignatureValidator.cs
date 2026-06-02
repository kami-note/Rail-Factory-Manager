using System.Security.Cryptography;
using System.Text;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

/// <summary>
/// PlugNotas sends the tenant's configured API key in the x-api-key header.
/// We compare it against the stored api_key credential using constant-time comparison.
/// </summary>
public sealed class PlugNotasWebhookSignatureValidator : IWebhookSignatureValidator
{
    public string Provider => "plugnotas";
    public string CredentialKey => "api_key";

    public bool IsValid(string rawBody, HttpRequest request, string storedSecret)
    {
        if (!request.Headers.TryGetValue("x-api-key", out var provided) || string.IsNullOrEmpty(provided))
            return false;

        // Pre-hash both sides so FixedTimeEquals always compares 32-byte digests,
        // preventing timing oracle on length mismatch.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString())),
            SHA256.HashData(Encoding.UTF8.GetBytes(storedSecret)));
    }
}
