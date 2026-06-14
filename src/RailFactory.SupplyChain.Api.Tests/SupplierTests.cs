using FluentAssertions;
using Xunit;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Supplier"/> aggregate root.
/// </summary>
public class SupplierTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var fiscalId = "12345678000190";
        var name = "  Global Parafusos Ltda  ";

        // Act
        var supplier = Supplier.Create(fiscalId, name);

        // Assert
        supplier.Id.Should().NotBeEmpty();
        supplier.FiscalId.Value.Should().Be("12345678000190");
        supplier.Name.Should().Be("Global Parafusos Ltda"); // trimmed
        supplier.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act
        Action act = () => Supplier.Create("12345678000190", invalidName);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
