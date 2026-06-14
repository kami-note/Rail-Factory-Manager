using FluentAssertions;
using Xunit;
using RailFactory.Iam.Api.Domain;

namespace RailFactory.Iam.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="IamApiKey"/> entity.
/// </summary>
public class IamApiKeyTests
{
    [Fact]
    public void Generate_ShouldReturnPlaintextKeyAndCorrectlyPopulatedEntity()
    {
        // Arrange
        var tenant = "dev";
        var name = "  M2M Integration Key  ";
        var email = "admin@rf.com";
        var perms = "[\"inventory.read\"]";
        var expires = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var (entity, plaintext) = IamApiKey.Generate(tenant, name, email, perms, expires);

        // Assert
        plaintext.Should().StartWith("rfk_");
        entity.Id.Should().NotBeEmpty();
        entity.TenantCode.Should().Be(tenant);
        entity.Name.Should().Be("M2M Integration Key"); // trimmed
        entity.CreatedByEmail.Should().Be(email);
        entity.PermissionsJson.Should().Be(perms);
        entity.ExpiresAt.Should().Be(expires);
        entity.IsActive.Should().BeTrue();
        entity.RevokedAt.Should().BeNull();
        entity.KeyPrefix.Should().Be(plaintext[..12]);
        entity.KeyHash.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("", "Key", "admin@rf.com")]
    [InlineData("dev", "", "admin@rf.com")]
    [InlineData("dev", "Key", "")]
    [InlineData("   ", "Key", "admin@rf.com")]
    public void Generate_WithInvalidArguments_ShouldThrowArgumentException(string tenant, string name, string email)
    {
        // Act
        Action act = () => IamApiKey.Generate(tenant, name, email, "[]");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Revoke_ShouldSetRevokedAtAndDeactivateKey()
    {
        // Arrange
        var (key, _) = IamApiKey.Generate("dev", "Key", "admin@rf.com", "[]");

        // Act
        key.Revoke();

        // Assert
        key.RevokedAt.Should().NotBeNull();
        key.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var (key, _) = IamApiKey.Generate("dev", "Key", "admin@rf.com", "[]");
        key.Revoke();

        // Act
        Action act = () => key.Revoke();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key is already revoked*");
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var (key, _) = IamApiKey.Generate("dev", "Key", "admin@rf.com", "[]", DateTimeOffset.UtcNow.AddMinutes(-5));

        // Assert
        key.IsActive.Should().BeFalse();
    }
}
