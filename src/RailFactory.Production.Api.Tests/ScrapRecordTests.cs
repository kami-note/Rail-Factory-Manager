using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="ScrapRecord"/> entity.
/// </summary>
public class ScrapRecordTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var opId = Guid.NewGuid();
        var matCode = "MAT-ACO-2MM";
        var qty = 1.2m;
        var uom = "  kg  ";
        var reason = "  Laser alignment error  ";

        // Act
        var record = ScrapRecord.Create(opId, matCode, qty, uom, reason);

        // Assert
        record.Id.Should().NotBeEmpty();
        record.ProductionOrderId.Should().Be(opId);
        record.MaterialCode.Value.Should().Be("MAT-ACO-2MM");
        record.ScrapQuantity.Should().Be(qty);
        record.UnitOfMeasure.Should().Be("KG"); // trimmed and uppercased
        record.Reason.Should().Be("Laser alignment error"); // trimmed
    }

    [Theory]
    [InlineData("", "KG", "reason")]
    [InlineData("MAT-1", "", "reason")]
    [InlineData("MAT-1", "KG", "")]
    [InlineData("   ", "KG", "reason")]
    [InlineData("MAT-1", "   ", "reason")]
    [InlineData("MAT-1", "KG", "   ")]
    public void Create_WithEmptyOrWhitespaceArguments_ShouldThrowArgumentException(string matCode, string uom, string reason)
    {
        // Act
        Action act = () => ScrapRecord.Create(Guid.NewGuid(), matCode, 5m, uom, reason);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2.5)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentException(decimal invalidQty)
    {
        // Act
        Action act = () => ScrapRecord.Create(Guid.NewGuid(), "MAT-1", invalidQty, "KG", "reason");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Scrap quantity must be greater than zero*");
    }
}
