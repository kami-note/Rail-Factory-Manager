using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using RailFactory.Tenancy.Api.Infrastructure;

namespace RailFactory.Tenancy.Api.Tests;

public class CredentialEncryptionServiceTests
{
    private static CredentialEncryptionService BuildSut(byte[]? kek = null)
    {
        kek ??= RandomNumberGenerator.GetBytes(32);
        var config = Substitute.For<IConfiguration>();
        config["Tenancy:Kek"].Returns(Convert.ToBase64String(kek));
        return new CredentialEncryptionService(config);
    }

    [Fact]
    public void RoundTrip_EncryptThenDecrypt_ReturnsOriginalCredentials()
    {
        var sut = BuildSut();
        var original = new Dictionary<string, string>
        {
            ["api_key"] = "super-secret-key",
            ["base_url"] = "https://api.provider.com"
        };

        var (ciphertext, wrappedDek, iv) = sut.EncryptCredentials(original);

        using var decrypted = sut.DecryptCredentials(ciphertext, wrappedDek, iv);

        decrypted.ToStringDictionary().Should().BeEquivalentTo(original);
    }

    [Fact]
    public void EncryptCredentials_ProducesDistinctCiphertextsForSameInput()
    {
        var sut = BuildSut();
        var credentials = new Dictionary<string, string> { ["k"] = "v" };

        var (ct1, _, _) = sut.EncryptCredentials(credentials);
        var (ct2, _, _) = sut.EncryptCredentials(credentials);

        ct1.Should().NotEqual(ct2, because: "each call uses a fresh random nonce");
    }

    [Fact]
    public void DecryptCredentials_ThrowsWhenTamperedCiphertext()
    {
        var sut = BuildSut();
        var (ciphertext, wrappedDek, iv) = sut.EncryptCredentials(
            new Dictionary<string, string> { ["k"] = "v" });

        ciphertext[0] ^= 0xFF; // corrupt the tag

        var act = () => sut.DecryptCredentials(ciphertext, wrappedDek, iv);

        act.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void DecryptCredentials_ThrowsWhenWrongKek()
    {
        var sut = BuildSut();
        var wrongKekSut = BuildSut(); // different random KEK
        var (ciphertext, wrappedDek, iv) = sut.EncryptCredentials(
            new Dictionary<string, string> { ["k"] = "v" });

        var act = () => wrongKekSut.DecryptCredentials(ciphertext, wrappedDek, iv);

        act.Should().Throw<AuthenticationTagMismatchException>();
    }

    [Fact]
    public void Constructor_ThrowsWhenKekTooShort()
    {
        var shortKek = RandomNumberGenerator.GetBytes(16);
        var config = Substitute.For<IConfiguration>();
        config["Tenancy:Kek"].Returns(Convert.ToBase64String(shortKek));

        var act = () => new CredentialEncryptionService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32 bytes*");
    }

    [Fact]
    public void Constructor_ThrowsWhenKekMissing()
    {
        var config = Substitute.For<IConfiguration>();
        config["Tenancy:Kek"].Returns((string?)null);

        var act = () => new CredentialEncryptionService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TENANCY__KEK*");
    }

    [Fact]
    public void DecryptCredentials_ReturnsSecureCredentialsThatCanBeDisposed()
    {
        var sut = BuildSut();
        var original = new Dictionary<string, string> { ["token"] = "abc" };
        var (ct, dek, iv) = sut.EncryptCredentials(original);

        var result = sut.DecryptCredentials(ct, dek, iv);
        result.GetString("token").Should().Be("abc");

        var dispose = () => result.Dispose();
        dispose.Should().NotThrow();
    }
}
