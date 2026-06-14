using FluentAssertions;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="TenantIntegration"/> entity.
/// </summary>
public class TenantIntegrationTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var tenantId = "acme-corp";
        var category = "payment";
        var provider = "asaas";
        var creds = new byte[] { 1, 2, 3 };
        var dek = new byte[] { 4, 5, 6 };
        var iv = new byte[] { 7, 8, 9 };

        // Act
        var integration = TenantIntegration.Create(tenantId, category, provider, creds, dek, iv);

        // Assert
        integration.Id.Should().NotBeEmpty();
        integration.TenantId.Should().Be(tenantId);
        integration.Category.Should().Be(category);
        integration.ProviderType.Should().Be(provider);
        integration.IsEnabled.Should().BeTrue();
        integration.EncryptedCredentials.Should().BeEquivalentTo(creds);
        integration.CredentialsDek.Should().BeEquivalentTo(dek);
        integration.CredentialsIv.Should().BeEquivalentTo(iv);
        integration.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("", "payment", "asaas")]
    [InlineData("acme", "", "asaas")]
    [InlineData("acme", "payment", "")]
    [InlineData("   ", "payment", "asaas")]
    public void Create_WithInvalidArguments_ShouldThrowArgumentException(string tenantId, string category, string provider)
    {
        // Act
        Action act = () => TenantIntegration.Create(tenantId, category, provider, new byte[0], new byte[0], new byte[0]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Disable_ShouldChangeIsEnabledToFalse()
    {
        // Arrange
        var integration = TenantIntegration.Create("acme", "payment", "asaas", new byte[0], new byte[0], new byte[0]);

        // Act
        integration.Disable();

        // Assert
        integration.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Enable_ShouldChangeIsEnabledToTrue_WhenDisabled()
    {
        // Arrange
        var integration = TenantIntegration.Create("acme", "payment", "asaas", new byte[0], new byte[0], new byte[0]);
        integration.Disable();

        // Act
        integration.Enable();

        // Assert
        integration.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void UpdateCredentials_ShouldUpdateFields()
    {
        // Arrange
        var integration = TenantIntegration.Create("acme", "payment", "asaas", new byte[0], new byte[0], new byte[0]);
        var newCreds = new byte[] { 10, 11 };
        var newDek = new byte[] { 12, 13 };
        var newIv = new byte[] { 14, 15 };

        // Act
        integration.UpdateCredentials("asaas-new", newCreds, newDek, newIv);

        // Assert
        integration.ProviderType.Should().Be("asaas-new");
        integration.EncryptedCredentials.Should().BeEquivalentTo(newCreds);
        integration.CredentialsDek.Should().BeEquivalentTo(newDek);
        integration.CredentialsIv.Should().BeEquivalentTo(newIv);
    }
}
