using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="BillOfMaterials"/> aggregate.
/// </summary>
public class BillOfMaterialsTests
{
    #region Batch Size Tests

    /// <summary>
    /// Verifies that creating a BOM without specifying batch size defaults to 1.0.
    /// </summary>
    [Fact]
    public void Create_WithoutBatchSize_ShouldDefaultToOne()
    {
        // Act
        var bom = BillOfMaterials.Create("MAT-ABC", 1);

        // Assert
        bom.BatchSize.Should().Be(1.0m);
    }

    /// <summary>
    /// Verifies that a BOM can be created with a valid custom batch size.
    /// </summary>
    [Fact]
    public void Create_WithValidBatchSize_ShouldSetBatchSize()
    {
        // Act
        var bom = BillOfMaterials.Create("MAT-ABC", 1, 100.5m);

        // Assert
        bom.BatchSize.Should().Be(100.5m);
    }

    /// <summary>
    /// Verifies that creating a BOM with zero or negative batch size throws an exception.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithInvalidBatchSize_ShouldThrowArgumentException(decimal invalidBatchSize)
    {
        // Act
        Action act = () => BillOfMaterials.Create("MAT-ABC", 1, invalidBatchSize);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Batch size must be greater than zero*");
    }

    #endregion

    #region Clone BOM Tests

    /// <summary>
    /// Verifies that cloning an existing BOM copies all properties and items as a Draft, including their scrap factors.
    /// </summary>
    [Fact]
    public void CloneAsDraft_ShouldCopyAllItemsAndProperties_AsDraft()
    {
        // Given
        var original = BillOfMaterials.Create("MAT-ABC", 1, 50.0m);
        original.AddItem("COMP-1", 10.0m, "KG", 0.05m);
        original.AddItem("COMP-2", 2.5m, "UN", 0.12m);

        // When
        var clone = original.CloneAsDraft(2);

        // Then
        clone.ProductCode.Value.Should().Be(original.ProductCode.Value);
        clone.Version.Should().Be(2);
        clone.Status.Should().Be(BomStatus.Draft);
        clone.BatchSize.Should().Be(original.BatchSize);
        clone.Items.Should().HaveCount(2);

        var firstItem = clone.Items.Should().ContainSingle(i => i.MaterialCode.Value == "COMP-1").Subject;
        firstItem.Quantity.Should().Be(10.0m);
        firstItem.UnitOfMeasure.Should().Be("KG");
        firstItem.ScrapFactor.Should().Be(0.05m);
        firstItem.BomId.Should().Be(clone.Id);

        var secondItem = clone.Items.Should().ContainSingle(i => i.MaterialCode.Value == "COMP-2").Subject;
        secondItem.Quantity.Should().Be(2.5m);
        secondItem.UnitOfMeasure.Should().Be("UN");
        secondItem.ScrapFactor.Should().Be(0.12m);
        secondItem.BomId.Should().Be(clone.Id);
    }

    /// <summary>
    /// Verifies that adding an item with an invalid scrap factor throws an exception.
    /// </summary>
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void AddItem_WithInvalidScrapFactor_ShouldThrowArgumentException(decimal invalidScrapFactor)
    {
        // Given
        var bom = BillOfMaterials.Create("MAT-ABC", 1);

        // Act
        Action act = () => bom.AddItem("COMP-1", 10.0m, "KG", invalidScrapFactor);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Scrap factor must be between 0*");
    }

    /// <summary>
    /// Verifies that cloning with a version equal or lower than the original throws an exception.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void CloneAsDraft_WithInvalidVersion_ShouldThrowArgumentException(int invalidVersion)
    {
        // Given
        var original = BillOfMaterials.Create("MAT-ABC", 2);

        // When
        Action act = () => original.CloneAsDraft(invalidVersion);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be greater than current version*");
    }

    #endregion
}
