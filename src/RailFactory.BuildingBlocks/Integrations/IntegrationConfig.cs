namespace RailFactory.BuildingBlocks.Integrations;

public sealed class IntegrationConfig(
    string providerType,
    SecureCredentials credentials,
    bool isEnabled) : IDisposable
{
    public string ProviderType { get; } = providerType;
    public SecureCredentials Credentials { get; } = credentials;
    public bool IsEnabled { get; } = isEnabled;

    public void Dispose() => Credentials.Dispose();
}
