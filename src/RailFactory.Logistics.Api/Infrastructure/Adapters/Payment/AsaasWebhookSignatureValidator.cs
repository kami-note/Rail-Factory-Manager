using System.Security.Cryptography;
using System.Text;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

/// <summary>
/// Asaas sends X-Asaas-Access-Token header containing the token configured in the webhook settings panel.
/// Validation is a constant-time comparison against the stored webhook_token credential.
/// </summary>
public sealed class AsaasWebhookSignatureValidator : IWebhookSignatureValidator
{
    public string Provider => "asaas";
    public string Category => "payment";
    public string CredentialKey => "webhook_token";

    public bool IsValid(string rawBody, HttpRequest request, string storedSecret)
    {
        if (!request.Headers.TryGetValue("X-Asaas-Access-Token", out var provided) || string.IsNullOrEmpty(provided))
            return false;

        // Use hash-of-hash trick (same as MelhorEnvioValidator) to prevent timing oracle on length mismatch.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString())),
            SHA256.HashData(Encoding.UTF8.GetBytes(storedSecret)));
    }
}
