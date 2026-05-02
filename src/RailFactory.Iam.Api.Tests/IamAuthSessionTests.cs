using System.Text.Json;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Iam.Api.Application.Auth;
using Xunit;

namespace RailFactory.Iam.Api.Tests;

public sealed class IamAuthSessionTests
{
    [Fact]
    public void AuthSessionDto_Unauthenticated_HasCanonical401Shape()
    {
        var json = JsonSerializer.Serialize(AuthSessionDto.Unauthenticated, JsonOptions);

        Assert.Contains("\"authenticated\":false", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"user\":null", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthSessionDto_Authenticated_HasCanonical200Shape()
    {
        var payload = AuthSessionDto.CreateAuthenticated("Tester", "tester@example.com");
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        Assert.Contains("\"authenticated\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tester@example.com", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalizeExternalLogin_WhenOAuthFails_NormalizesCode()
    {
        var sut = new FinalizeExternalLogin();

        var result = sut.Execute(false, "dev", "/app", "Some Error");

        Assert.False(result.Success);
        Assert.Equal("some_error", result.ErrorCode);
    }

    [Fact]
    public async Task UpsertLocalUserFromExternalLogin_WhenSubjectMissing_FailsWithOAuthError()
    {
        var sut = new UpsertLocalUserFromExternalLogin(new FakeIamLocalUserRepository());

        var result = await sut.ExecuteAsync(
            tenantCode: "dev",
            externalProvider: "google",
            externalSubject: null,
            email: "user@example.com",
            displayName: "User",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(AuthResultErrorCode.OAuthError, result.ErrorCode);
    }

    [Fact]
    public async Task UpsertLocalUserFromExternalLogin_WhenValid_PersistsUser()
    {
        var repository = new FakeIamLocalUserRepository();
        var sut = new UpsertLocalUserFromExternalLogin(repository);

        var result = await sut.ExecuteAsync(
            tenantCode: "dev",
            externalProvider: "google",
            externalSubject: "subject-123",
            email: "user@example.com",
            displayName: "User",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(repository.Users);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class FakeIamLocalUserRepository : IIamLocalUserRepository
    {
        public List<IamLocalUser> Users { get; } = [];

        public Task UpsertAsync(IamLocalUser user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }
    }
}
