using FluentAssertions;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Carrier"/> entity.
/// </summary>
public class CarrierTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "  TransRapido Ltda  ";
        var doc = "  12.345.678/0001-90  ";
        var email = "contact@transrapido.com";
        var rateKg = 1.5m;
        var rateCbm = 150m;
        var webhook = "http://my-webhook.com/event";

        // Act
        var carrier = Carrier.Create(name, doc, email, rateKg, rateCbm, webhook);

        // Assert
        carrier.Id.Should().NotBeEmpty();
        carrier.Name.Should().Be("TransRapido Ltda"); // Trimming check
        carrier.DocumentNumber.Should().Be("12.345.678/0001-90"); // Trimming check
        carrier.ContactEmail.Should().Be(email);
        carrier.WebhookUrl.Should().Be(webhook);
        carrier.RatePerKg.Should().Be(rateKg);
        carrier.RatePerCbm.Should().Be(rateCbm);
        carrier.Status.Should().Be(CarrierStatus.Active);
    }

    [Theory]
    [InlineData("", "12.345.678/0001-90")]
    [InlineData("TransRapido", "")]
    [InlineData("   ", "12.345.678/0001-90")]
    [InlineData("TransRapido", "   ")]
    public void Create_WithEmptyOrWhitespaceArguments_ShouldThrowArgumentException(string name, string doc)
    {
        // Act
        Action act = () => Carrier.Create(name, doc, "email", 1m, 10m);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetWebhookUrl_ShouldSetAndTrimUrl()
    {
        // Arrange
        var carrier = Carrier.Create("Trans", "123", null, 1m, 1m);

        // Act
        carrier.SetWebhookUrl("  https://webhook.site/abc  ");

        // Assert
        carrier.WebhookUrl.Should().Be("https://webhook.site/abc");
    }

    [Fact]
    public void Deactivate_ShouldChangeStatusToInactive()
    {
        // Arrange
        var carrier = Carrier.Create("Trans", "123", null, 1m, 1m);

        // Act
        carrier.Deactivate();

        // Assert
        carrier.Status.Should().Be(CarrierStatus.Inactive);
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenInactive()
    {
        // Arrange
        var carrier = Carrier.Create("Trans", "123", null, 1m, 1m);
        carrier.Deactivate();

        // Act
        carrier.Activate();

        // Assert
        carrier.Status.Should().Be(CarrierStatus.Active);
    }
}
