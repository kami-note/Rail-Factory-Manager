using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="QualityInspection"/> entity.
/// </summary>
public class QualityInspectionTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var opId = Guid.NewGuid();
        var result = InspectionResult.Passed;
        var inspector = "  Ana Quality  ";
        var notes = "  All measurements within tolerance  ";

        // Act
        var inspection = QualityInspection.Create(opId, result, inspector, notes);

        // Assert
        inspection.Id.Should().NotBeEmpty();
        inspection.ProductionOrderId.Should().Be(opId);
        inspection.Result.Should().Be(result);
        inspection.InspectedBy.Should().Be("Ana Quality"); // trimmed
        inspection.Notes.Should().Be("All measurements within tolerance"); // trimmed
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidInspector_ShouldThrowArgumentException(string invalidInspector)
    {
        // Act
        Action act = () => QualityInspection.Create(Guid.NewGuid(), InspectionResult.Passed, invalidInspector, null);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
