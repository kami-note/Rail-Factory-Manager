using RailFactory.Frontend.Api;
using Xunit;

namespace RailFactory.Frontend.Tests;

public sealed class MaterialImageUploadPolicyTests
{
    [Theory]
    [InlineData("image/png", ".png")]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/webp", ".webp")]
    public void TryGetExtension_AllowsExpectedMimeTypes(string contentType, string expectedExtension)
    {
        var allowed = MaterialImageUploadPolicy.TryGetExtension(contentType, out var extension);

        Assert.True(allowed);
        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public void TryGetExtension_RejectsUnsupportedMimeTypes(string contentType)
    {
        var allowed = MaterialImageUploadPolicy.TryGetExtension(contentType, out _);
        Assert.False(allowed);
    }

    [Fact]
    public void MaxUploadBytes_IsConfiguredToFiveMegabytes()
    {
        Assert.Equal(5 * 1024 * 1024, MaterialImageUploadPolicy.MaxUploadBytes);
    }
}
