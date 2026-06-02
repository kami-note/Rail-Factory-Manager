using System.Security.Cryptography;
using System.Text;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

/// <summary>
/// FocusNFe does not use HMAC. The webhook callback URL includes a shared secret as
/// a query parameter (?secret=value). We compare the incoming query param against the
/// stored webhook_secret credential using constant-time comparison.
/// </summary>
public sealed class FocusNfeWebhookSignatureValidator : IWebhookSignatureValidator
{
    public string Provider => "focusnfe";
    public string CredentialKey => "webhook_secret";

    public bool IsValid(string rawBody, HttpRequest request, string storedSecret)
    {
        if (string.IsNullOrEmpty(storedSecret))
            return false;

        var provided = request.Query.TryGetValue("secret", out var q) ? q.ToString() : string.Empty;
        if (string.IsNullOrEmpty(provided))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(storedSecret)));
    }
}
