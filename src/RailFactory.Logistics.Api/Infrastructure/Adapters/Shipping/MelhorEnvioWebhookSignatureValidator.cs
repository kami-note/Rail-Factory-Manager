using System.Security.Cryptography;
using System.Text;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

/// <summary>
/// Melhor Envio sends X-ME-Signature = HMAC-SHA256(rawBody, client_secret).
/// </summary>
public sealed class MelhorEnvioWebhookSignatureValidator : IWebhookSignatureValidator
{
    public string Provider => "melhorenvio";
    public string Category => "shipping";
    public string CredentialKey => "client_secret";

    public bool IsValid(string rawBody, HttpRequest request, string storedSecret)
    {
        if (!request.Headers.TryGetValue("X-ME-Signature", out var provided) || string.IsNullOrEmpty(provided))
            return false;

        var expectedBytes = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(storedSecret),
            Encoding.UTF8.GetBytes(rawBody));
        var expected = Convert.ToHexString(expectedBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided.ToString().ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expected));
    }
}
