using FluentAssertions;
using NSubstitute;
using RailFactory.Inventory.Api.Application.Materials;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Tests;

public class RegisterSupplierMaterialMappingTests
{
    private readonly IMaterialRepository _repository = Substitute.For<IMaterialRepository>();
    private readonly RegisterSupplierMaterialMapping _sut;

    public RegisterSupplierMaterialMappingTests()
    {
        _sut = new RegisterSupplierMaterialMapping(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpsertHintAndSaveChanges()
    {
        // Arrange
        var input = new RegisterSupplierMaterialMappingInput("12345678901234", "SUP-PROD-01", "MAT-01");

        // Act
        var result = await _sut.ExecuteAsync(input, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        await _repository.Received(1).UpsertSupplierMaterialHintAsync(
            Arg.Is<SupplierMaterialHint>(h => 
                h.SupplierFiscalId.Value == "12345678901234" && 
                h.SupplierProductCode == "SUP-PROD-01" && 
                h.MappedMaterialCode.Value == "MAT-01"), 
            Arg.Any<CancellationToken>());
            
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
