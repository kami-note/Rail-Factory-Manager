using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Api;
using Xunit;

namespace RailFactory.Frontend.Tests;

public sealed class FrontendAuthSessionContractTests
{
    [Theory]
    [InlineData(401, "unauthorized")]
    [InlineData(403, "csrf_error")]
    [InlineData(400, "tenant_error")]
    [InlineData(404, "tenant_error")]
    [InlineData(500, "oauth_error")]
    public void AuthUiErrorMapper_MapsExpectedErrorCode(int statusCode, string expectedCode)
    {
        var result = AuthUiErrorMapper.MapFromStatusCode(statusCode);

        Assert.Equal(expectedCode, result.Code);
    }

    [Fact]
    public void SharedAuthSessionDto_ProducesStableUnauthenticatedPayload()
    {
        var payload = AuthSessionDto.Unauthenticated;

        Assert.False(payload.Authenticated);
        Assert.Null(payload.User);
    }
}
