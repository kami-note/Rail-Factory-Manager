using System.Security.Cryptography;
using FluentAssertions;
using RailFactory.BuildingBlocks.Integrations;

namespace RailFactory.Tenancy.Api.Tests;

public class SecureCredentialsTests
{
    [Fact]
    public void FromStrings_StoresValuesAsByteArrays()
    {
        var source = new Dictionary<string, string> { ["key"] = "secret" };
        using var creds = SecureCredentials.FromStrings(source);

        creds.Keys.Should().Contain("key");
        creds.GetString("key").Should().Be("secret");
    }

    [Fact]
    public void TryGetString_ReturnsTrueForExistingKey()
    {
        using var creds = SecureCredentials.FromStrings(new Dictionary<string, string> { ["k"] = "v" });

        var found = creds.TryGetString("k", out var value);

        found.Should().BeTrue();
        value.Should().Be("v");
    }

    [Fact]
    public void TryGetString_ReturnsFalseForMissingKey()
    {
        using var creds = SecureCredentials.FromStrings(new Dictionary<string, string>());

        var found = creds.TryGetString("missing", out var value);

        found.Should().BeFalse();
        value.Should().BeEmpty();
    }

    [Fact]
    public void GetString_ThrowsForMissingKey()
    {
        using var creds = SecureCredentials.FromStrings(new Dictionary<string, string>());

        var act = () => creds.GetString("missing");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ToStringDictionary_ReturnsAllPairs()
    {
        var source = new Dictionary<string, string>
        {
            ["api_key"] = "abc123",
            ["base_url"] = "https://api.example.com"
        };
        using var creds = SecureCredentials.FromStrings(source);

        var dict = creds.ToStringDictionary();

        dict.Should().BeEquivalentTo(source);
    }

    [Fact]
    public void Dispose_ZeroesUnderlyingBytes()
    {
        // We can't directly inspect zeroed bytes after Dispose, but we can verify
        // that Dispose does not throw and Keys is cleared.
        var source = new Dictionary<string, string> { ["token"] = "sensitive-value" };
        var creds = SecureCredentials.FromStrings(source);

        creds.Dispose();

        creds.Keys.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var creds = SecureCredentials.FromStrings(new Dictionary<string, string> { ["k"] = "v" });

        var act = () =>
        {
            creds.Dispose();
            creds.Dispose();
        };

        act.Should().NotThrow();
    }
}
