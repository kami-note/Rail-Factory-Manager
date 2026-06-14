using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="WorkCenter"/> aggregate root.
/// </summary>
public class WorkCenterTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var code = "  wc-cor-01  ";
        var name = "  Corte a Laser  ";

        // Act
        var wc = WorkCenter.Create(code, name);

        // Assert
        wc.Id.Should().NotBeEmpty();
        wc.Code.Should().Be("WC-COR-01"); // trimmed and uppercased
        wc.Name.Should().Be("Corte a Laser"); // trimmed
        wc.Status.Should().Be(WorkCenterStatus.Active);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("Code", "")]
    [InlineData("   ", "Name")]
    [InlineData("Code", "   ")]
    public void Create_WithInvalidArguments_ShouldThrowArgumentException(string code, string name)
    {
        // Act
        Action act = () => WorkCenter.Create(code, name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldChangeStatusToInactive()
    {
        // Arrange
        var wc = WorkCenter.Create("WC-01", "Workstation 1");

        // Act
        wc.Deactivate();

        // Assert
        wc.Status.Should().Be(WorkCenterStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var wc = WorkCenter.Create("WC-01", "Workstation 1");
        wc.Deactivate();

        // Act
        Action act = () => wc.Deactivate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenInactive()
    {
        // Arrange
        var wc = WorkCenter.Create("WC-01", "Workstation 1");
        wc.Deactivate();

        // Act
        wc.Activate();

        // Assert
        wc.Status.Should().Be(WorkCenterStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var wc = WorkCenter.Create("WC-01", "Workstation 1");

        // Act
        Action act = () => wc.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already active*");
    }
}
