using FluentAssertions;
using NSubstitute;
using RailFactory.BuildingBlocks.Domain;
using RailFactory.Production.Api.Application.Boms;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="GetBomCostRollup"/> use case.
/// </summary>
public class GetBomCostRollupTests
{
    private readonly IBomRepository _repository = Substitute.For<IBomRepository>();
    private readonly IMaterialCostProvider _costProvider = Substitute.For<IMaterialCostProvider>();
    private readonly GetBomCostRollup _sut;

    public GetBomCostRollupTests()
    {
        _sut = new GetBomCostRollup(_repository, _costProvider);
    }

    /// <summary>
    /// Verifies that a KeyNotFoundException is thrown when requesting a costing rollup for a non-existent BOM.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenBomNotFound_ShouldThrowKeyNotFoundException()
    {
        // Given
        var invalidId = Guid.NewGuid();
        _repository.GetByIdAsync(invalidId, Arg.Any<CancellationToken>())
            .Returns((BillOfMaterials?)null);

        // When
        Func<Task> act = async () => await _sut.ExecuteAsync(invalidId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*BOM '{invalidId}' not found.*");
    }

    /// <summary>
    /// Verifies that costing rollup correctly retrieves prices, scales quantity by batch size,
    /// applies scrap factor, and sums up total cost.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenBomExists_ShouldCalculateTotalCostAndItemsCorrectly()
    {
        // Given
        var bomId = Guid.NewGuid();
        var bom = BillOfMaterials.Create("FINISHED-PROD", 1, 2.0m); // Batch size 2.0
        
        bom.AddItem("COMP-1", 4.0m, "UN", 0.10m); // Qty per batch = 4.0. Scrap = 10%. (Scaled qty per unit = 2.2)
        bom.AddItem("COMP-2", 1.0m, "KG", 0.00m); // Qty per batch = 1.0. Scrap = 0%. (Scaled qty per unit = 0.5)

        // Reflecting the Guid PK to match what EF Core does
        var idProperty = typeof(Entity<Guid>).GetProperty("Id");
        idProperty?.GetSetMethod(nonPublic: true)?.Invoke(bom, [bomId]);

        _repository.GetByIdAsync(bomId, Arg.Any<CancellationToken>())
            .Returns(bom);

        var costs = new Dictionary<string, decimal>
        {
            { "COMP-1", 10.0m },  // Unit price = 10.0
            { "COMP-2", 50.0m }   // Unit price = 50.0
        };

        _costProvider.GetMaterialCostsAsync(
            Arg.Is<IEnumerable<string>>(x => x.Contains("COMP-1") && x.Contains("COMP-2")),
            Arg.Any<CancellationToken>()
        ).Returns(costs);

        // When
        var result = await _sut.ExecuteAsync(bomId, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.BomId.Should().Be(bomId);
        result.ProductCode.Should().Be("FINISHED-PROD");
        result.BatchSize.Should().Be(2.0m);
        
        // Expected COMP-1 contribution: (4.0 / 2.0) * (1 + 0.10) * 10.0 = 22.0
        // Expected COMP-2 contribution: (1.0 / 2.0) * (1 + 0.00) * 50.0 = 25.0
        // Total cost: 22.0 + 25.0 = 47.0
        result.TotalEstimatedCost.Should().Be(47.0m);

        result.Items.Should().HaveCount(2);

        var firstItem = result.Items.Should().ContainSingle(i => i.MaterialCode == "COMP-1").Subject;
        firstItem.Quantity.Should().Be(4.0m);
        firstItem.UnitOfMeasure.Should().Be("UN");
        firstItem.ScrapFactor.Should().Be(0.10m);
        firstItem.ScaledQuantity.Should().Be(2.2m);
        firstItem.UnitPrice.Should().Be(10.0m);
        firstItem.TotalCost.Should().Be(22.0m);

        var secondItem = result.Items.Should().ContainSingle(i => i.MaterialCode == "COMP-2").Subject;
        secondItem.Quantity.Should().Be(1.0m);
        secondItem.UnitOfMeasure.Should().Be("KG");
        secondItem.ScrapFactor.Should().Be(0.00m);
        secondItem.ScaledQuantity.Should().Be(0.5m);
        secondItem.UnitPrice.Should().Be(50.0m);
        secondItem.TotalCost.Should().Be(25.0m);
    }
}
