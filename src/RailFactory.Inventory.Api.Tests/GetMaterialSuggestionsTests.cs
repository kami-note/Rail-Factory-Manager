using FluentAssertions;
using NSubstitute;
using RailFactory.Inventory.Api.Application.Materials;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Tests;

public class GetMaterialSuggestionsTests
{
    private readonly IMaterialRepository _repository = Substitute.For<IMaterialRepository>();
    private readonly GetMaterialSuggestions _sut;

    public GetMaterialSuggestionsTests()
    {
        _sut = new GetMaterialSuggestions(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedSuggestions()
    {
        // Arrange
        var input = new GetMaterialSuggestionsInput("123", "ncm", "desc", "123", "prod");
        var material = Material.Create(
            materialCode: "M01",
            officialName: "Test Material",
            description: "Description",
            category: MaterialCategory.RawMaterial,
            procurementType: ProcurementType.Buy,
            createdBy: EmailAddress.From("user@test.com"),
            unitOfMeasure: "UN",
            status: MaterialStatus.Verified,
            imageUrl: null,
            ncm: "ncm",
            gtin: "123"
        );
        
        var suggestions = new List<SupplierMaterialHintResult>
        {
            new(material, "High", "GTIN Match")
        };

        _repository.GetSuggestionsAsync("123", "ncm", "desc", "123", "prod", Arg.Any<CancellationToken>())
            .Returns(suggestions);

        // Act
        var result = await _sut.ExecuteAsync(input, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ConfidenceRank.Should().Be("High");
        result.First().Reason.Should().Be("GTIN Match");
        result.First().Material.MaterialCode.Should().Be("M01");
    }
}
