namespace RailFactory.Tenancy.Api.Api.Requests;

public sealed record ConfigureIntegrationRequest(
    string Category,
    string ProviderType,
    Dictionary<string, string> Credentials);
