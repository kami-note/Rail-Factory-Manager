using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace RailFactory.Frontend.Tests;

public sealed class FrontendRootRedirectTests : IClassFixture<WebApplicationFactory<Program>>
{
    static FrontendRootRedirectTests()
    {
        Environment.SetEnvironmentVariable("InternalToken__SigningKey", "a-very-long-signing-key-with-at-least-32-bytes-for-hmac-sha-256");
        Environment.SetEnvironmentVariable("InternalToken__Issuer", "railfactory.frontend.test");
        Environment.SetEnvironmentVariable("InternalToken__Audience", "railfactory.internal.test");
        Environment.SetEnvironmentVariable("InternalToken__LifetimeMinutes", "5");
    }

    private readonly WebApplicationFactory<Program> _factory;

    public FrontendRootRedirectTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoot_DoesNotReturnRedirect()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        // Before the fix, the root path '/' returned a 302 Found redirect to /api/frontend/status.
        // After the fix, it should not redirect, allowing index.html or fallback handler to serve the SPA.
        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.RedirectKeepVerb, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.RedirectMethod, response.StatusCode);
    }
}
