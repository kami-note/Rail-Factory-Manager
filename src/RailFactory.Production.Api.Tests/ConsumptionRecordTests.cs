using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="ConsumptionRecord"/> entity.
/// </summary>
public class ConsumptionRecordTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var opId = Guid.NewGuid();
        var matCode = "MAT-ACO-2MM";
        var qty = 15.5m;
        var uom = "  kg  ";
        var balanceId = Guid.NewGuid();

        // Act
        var record = ConsumptionRecord.Create(opId, matCode, qty, uom, balanceId);

        // Assert
        record.Id.Should().NotBeEmpty();
        record.ProductionOrderId.Should().Be(opId);
        record.MaterialCode.Value.Should().Be("MAT-ACO-2MM");
        record.ConsumedQuantity.Should().Be(qty);
        record.UnitOfMeasure.Should().Be("KG"); // trimmed and uppercased
        record.InventoryBalanceId.Should().Be(balanceId);
    }

    [Theory]
    [InlineData("", "KG")]
    [InlineData("MAT-1", "")]
    [InlineData("   ", "KG")]
    [InlineData("MAT-1", "   ")]
    public void Create_WithEmptyOrWhitespaceArguments_ShouldThrowArgumentException(string matCode, string uom)
    {
        // Act
        Action act = () => ConsumptionRecord.Create(Guid.NewGuid(), matCode, 10m, uom);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentException(decimal invalidQty)
    {
        // Act
        Action act = () => ConsumptionRecord.Create(Guid.NewGuid(), "MAT-1", invalidQty, "KG");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Consumed quantity must be greater than zero*");
    }
}
