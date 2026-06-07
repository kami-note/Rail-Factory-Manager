namespace RailFactory.Logistics.Api.Application.Ports;

public interface IWebhookSignatureValidator
{
    string Provider { get; }

    /// <summary>The integration category used to resolve credentials from Tenancy API (e.g. "fiscal", "shipping").</summary>
    string Category { get; }

    /// <summary>
    /// The credential key used to look up the signing secret in tenant integration credentials
    /// (e.g. "api_key" for PlugNotas, "webhook_secret" for FocusNFe).
    /// </summary>
    string CredentialKey { get; }

    bool IsValid(string rawBody, HttpRequest request, string storedSecret);
}
