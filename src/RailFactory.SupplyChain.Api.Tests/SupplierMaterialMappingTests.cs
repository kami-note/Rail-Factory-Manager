using FluentAssertions;
using Xunit;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="SupplierMaterialMapping"/> aggregate root.
/// </summary>
public class SupplierMaterialMappingTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var fiscalId = FiscalId.From("12345678000190");
        var supplierProd = "  cProd-abc  ";
        var internalCode = MaterialCode.From("MAT-ACO-2MM");
        var internalUom = "  kg  ";
        var supplierUnit = "  cx  ";
        var factor = 10.5m;
        var email = EmailAddress.From("operator@rf.com");

        // Act
        var mapping = SupplierMaterialMapping.Create(fiscalId, supplierProd, internalCode, internalUom, supplierUnit, factor, email);

        // Assert
        mapping.Id.Should().NotBeEmpty();
        mapping.SupplierFiscalId.Should().Be(fiscalId);
        mapping.SupplierProductCode.Should().Be("cProd-abc"); // trimmed
        mapping.InternalMaterialCode.Should().Be(internalCode);
        mapping.InternalUnitOfMeasure.Should().Be("KG"); // trimmed and uppercased
        mapping.SupplierUnit.Should().Be("CX"); // trimmed and uppercased
        mapping.ConversionFactor.Should().Be(factor);
        mapping.CreatedBy.Should().Be(email);
        mapping.LastModifiedBy.Should().Be(email);
    }

    [Theory]
    [InlineData("", "CX", "KG")]
    [InlineData("cProd", "", "KG")]
    [InlineData("cProd", "CX", "")]
    [InlineData("   ", "CX", "KG")]
    public void Create_WithInvalidArguments_ShouldThrowArgumentException(string prodCode, string supplierUnit, string internalUom)
    {
        // Act
        Action act = () => SupplierMaterialMapping.Create(
            FiscalId.From("12345678000190"), prodCode, MaterialCode.From("MAT"), internalUom, supplierUnit, 1m, EmailAddress.From("op@rf.com")
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithInvalidConversionFactor_ShouldThrowArgumentException(decimal invalidFactor)
    {
        // Act
        Action act = () => SupplierMaterialMapping.Create(
            FiscalId.From("12345678000190"), "prod", MaterialCode.From("MAT"), "KG", "CX", invalidFactor, EmailAddress.From("op@rf.com")
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Conversion factor must be greater than zero*");
    }

    [Fact]
    public void CorrectMapping_ShouldUpdateProperties()
    {
        // Arrange
        var mapping = SupplierMaterialMapping.Create(
            FiscalId.From("12345678000190"), "prod", MaterialCode.From("MAT-OLD"), "KG", "CX", 1m, EmailAddress.From("op@rf.com")
        );
        var newCode = MaterialCode.From("MAT-NEW");
        var newUom = "  un  ";
        var newFactor = 5.5m;
        var supervisor = EmailAddress.From("supervisor@rf.com");

        // Act
        mapping.CorrectMapping(newCode, newUom, newFactor, supervisor);

        // Assert
        mapping.InternalMaterialCode.Should().Be(newCode);
        mapping.InternalUnitOfMeasure.Should().Be("UN"); // trimmed and uppercased
        mapping.ConversionFactor.Should().Be(newFactor);
        mapping.LastModifiedBy.Should().Be(supervisor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CorrectMapping_WithInvalidConversionFactor_ShouldThrowArgumentException(decimal invalidFactor)
    {
        // Arrange
        var mapping = SupplierMaterialMapping.Create(
            FiscalId.From("12345678000190"), "prod", MaterialCode.From("MAT-OLD"), "KG", "CX", 1m, EmailAddress.From("op@rf.com")
        );

        // Act
        Action act = () => mapping.CorrectMapping(MaterialCode.From("MAT-NEW"), "UN", invalidFactor, EmailAddress.From("op@rf.com"));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Conversion factor must be greater than zero*");
    }
}
