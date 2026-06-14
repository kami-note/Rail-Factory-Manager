using FluentAssertions;
using RailFactory.Tenancy.Api.Domain;

namespace RailFactory.Tenancy.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Tenant"/> aggregate root.
/// </summary>
public class TenantTests
{
    [Fact]
    public void Register_WithValidData_ShouldInitializeCorrectlyAndRaiseEvent()
    {
        // Arrange
        var code = "  AcMe-Corp  ";
        var name = "  ACME Corporation  ";
        var locale = "pt-BR";
        var timeZone = "America/Sao_Paulo";
        var connStrings = new Dictionary<string, string> { { "db", "Host=localhost" } };

        // Act
        var tenant = Tenant.Register(code, name, locale, timeZone, connStrings);

        // Assert
        tenant.Code.Should().Be("acme-corp"); // normalized to lowercase
        tenant.DisplayName.Should().Be("ACME Corporation"); // trimmed
        tenant.Locale.Should().Be("pt-BR");
        tenant.TimeZone.Should().Be("America/Sao_Paulo");
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.IsActive.Should().BeTrue();
        tenant.ConnectionStrings.Should().ContainKey("db").WhoseValue.Should().Be("Host=localhost");

        tenant.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TenantRegisteredDomainEvent>()
            .Which.TenantCode.Should().Be("acme-corp");
    }

    [Theory]
    [InlineData("", "Name", "pt-BR", "UTC")]
    [InlineData("acme", "", "pt-BR", "UTC")]
    [InlineData("acme", "Name", "", "UTC")]
    [InlineData("acme", "Name", "pt-BR", "")]
    [InlineData("   ", "Name", "pt-BR", "UTC")]
    public void Register_WithInvalidArguments_ShouldThrowArgumentException(string code, string name, string locale, string tz)
    {
        // Act
        Action act = () => Tenant.Register(code, name, locale, tz, new Dictionary<string, string>());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetConnectionString_ShouldAddOrUpdateKey()
    {
        // Arrange
        var tenant = Tenant.Register("acme", "ACME", "pt-BR", "UTC", new Dictionary<string, string>());

        // Act
        tenant.SetConnectionString("logisticsdb", "Host=127.0.0.1");

        // Assert
        tenant.ConnectionStrings.Should().ContainKey("logisticsdb").WhoseValue.Should().Be("Host=127.0.0.1");
    }

    [Theory]
    [InlineData("", "Host")]
    [InlineData("db", "")]
    [InlineData("   ", "Host")]
    [InlineData("db", "   ")]
    public void SetConnectionString_WithInvalidArguments_ShouldThrowArgumentException(string service, string conn)
    {
        // Arrange
        var tenant = Tenant.Register("acme", "ACME", "pt-BR", "UTC", new Dictionary<string, string>());

        // Act
        Action act = () => tenant.SetConnectionString(service, conn);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
