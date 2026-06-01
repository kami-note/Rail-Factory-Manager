using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Application.Ports;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure;

namespace RailFactory.Tenancy.Api.Tests;

public class GetIntegrationCredentialsTests
{
    private readonly ITenantIntegrationRepository _repository =
        Substitute.For<ITenantIntegrationRepository>();

    private readonly CredentialEncryptionService _encryption;
    private readonly GetIntegrationCredentials _sut;

    public GetIntegrationCredentialsTests()
    {
        var kek = RandomNumberGenerator.GetBytes(32);
        var config = Substitute.For<IConfiguration>();
        config["Tenancy:Kek"].Returns(Convert.ToBase64String(kek));
        _encryption = new CredentialEncryptionService(config);
        _sut = new GetIntegrationCredentials(_repository, _encryption);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIntegrationNotFound_ReturnsNotFound()
    {
        _repository.FindAsync("tenant-1", "fiscal", Arg.Any<CancellationToken>())
            .Returns((TenantIntegration?)null);

        var result = await _sut.ExecuteAsync("tenant-1", "fiscal");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("integration.not_found");
    }

    [Fact]
    public async Task ExecuteAsync_WhenIntegrationDisabled_ReturnsNotAvailable()
    {
        var integration = BuildIntegration(isEnabled: false);
        _repository.FindAsync("tenant-1", "fiscal", Arg.Any<CancellationToken>())
            .Returns(integration);

        var result = await _sut.ExecuteAsync("tenant-1", "fiscal");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("integration.not_available");
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ReturnsDecryptedCredentials()
    {
        var original = new Dictionary<string, string>
        {
            ["api_key"] = "test-key-123",
            ["sandbox"] = "true"
        };
        var integration = BuildIntegration(isEnabled: true, credentials: original);
        _repository.FindAsync("tenant-1", "fiscal", Arg.Any<CancellationToken>())
            .Returns(integration);

        var result = await _sut.ExecuteAsync("tenant-1", "fiscal");

        result.IsSuccess.Should().BeTrue();
        using var details = result.Value;
        details.ProviderType.Should().Be("plugnotas");
        details.Credentials.ToStringDictionary().Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task ExecuteAsync_CredentialsAreDisposableAfterUse()
    {
        var integration = BuildIntegration(isEnabled: true);
        _repository.FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(integration);

        var result = await _sut.ExecuteAsync("tenant-1", "fiscal");

        result.IsSuccess.Should().BeTrue();
        var act = () => result.Value.Dispose();
        act.Should().NotThrow();
    }

    private TenantIntegration BuildIntegration(
        bool isEnabled,
        Dictionary<string, string>? credentials = null)
    {
        credentials ??= new Dictionary<string, string> { ["api_key"] = "default" };
        var (ct, dek, iv) = _encryption.EncryptCredentials(credentials);
        var integration = TenantIntegration.Create("tenant-1", "fiscal", "plugnotas", ct, dek, iv);
        if (!isEnabled) integration.Disable();
        return integration;
    }
}
