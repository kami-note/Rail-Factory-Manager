using FluentAssertions;
using NSubstitute;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ListInventoryBalances"/> query service.
/// </summary>
public class ListInventoryBalancesTests
{
    private readonly IInventoryRepository _repository = Substitute.For<IInventoryRepository>();
    private readonly IMaterialRepository _materialRepository = Substitute.For<IMaterialRepository>();
    private readonly ListInventoryBalances _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListInventoryBalancesTests"/> class.
    /// </summary>
    public ListInventoryBalancesTests()
    {
        _sut = new ListInventoryBalances(_repository, _materialRepository);
    }

    /// <summary>
    /// Verifies that <see cref="ListInventoryBalances.ExecuteAsync"/> returns active stock balances
    /// and appends synthetic zero-quantity balances for materials in the catalog that have no stock.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnActiveBalancesAndAppendSyntheticZeroBalances()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var activeBalance1 = InventoryBalance.CreatePending(
            "MAT-ACO-1020", "KG", 500m, Guid.NewGuid(), "receipt-1:item-1", null, null, InventorySourceType.Purchase, null);
        // Transition it to Available
        activeBalance1.Confirm(500m, null, null, isApproved: true);

        var activeBalances = new List<InventoryBalance> { activeBalance1 };

        var material1 = Material.Create(
            "MAT-ACO-1020", "Chapa de Aco", "Chapa de Aco", MaterialCategory.RawMaterial, ProcurementType.Buy, EmailAddress.From("system@railfactory.local"), "KG");

        var material2 = Material.Create(
            "MAT-212", "Tinta Verde", "Tinta Verde", MaterialCategory.RawMaterial, ProcurementType.Buy, EmailAddress.From("system@railfactory.local"), "L");

        var allMaterials = new List<Material> { material1, material2 };

        _repository.ListBalancesAsync(null, null, cancellationToken).Returns(activeBalances);
        _materialRepository.ListAllAsync(cancellationToken).Returns(allMaterials);

        // Act
        var result = await _sut.ExecuteAsync(null, null, cancellationToken);

        // Assert
        result.Should().HaveCount(2);

        // active balance checks
        var res1 = result.Single(r => r.MaterialCode == "MAT-ACO-1020");
        res1.Quantity.Should().Be(500m);
        res1.Status.Key.Should().Be(InventoryBalanceStatus.Available.ToString());

        // synthetic zero-balance checks
        var res2 = result.Single(r => r.MaterialCode == "MAT-212");
        res2.Quantity.Should().Be(0m);
        res2.Status.Key.Should().Be(InventoryBalanceStatus.Available.ToString());
        res2.SourceReference.Should().Be("catalog-init:MAT-212");
    }

    /// <summary>
    /// Verifies that when a sourceType filter is applied, the returned list contains only
    /// the materials/balances corresponding to that source (e.g., Purchase maps to RawMaterial).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithSourceTypeFilter_ShouldOnlyIncludeMatchingCategoryMaterials()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        var activeBalances = new List<InventoryBalance>(); // no active balances

        var rawMaterial = Material.Create(
            "MAT-212", "Tinta Verde", "Tinta Verde", MaterialCategory.RawMaterial, ProcurementType.Buy, EmailAddress.From("system@railfactory.local"), "L");

        var finishedGood = Material.Create(
            "ROL-01", "Rolamento", "Rolamento", MaterialCategory.FinishedGood, ProcurementType.Make, EmailAddress.From("system@railfactory.local"), "UN");

        var allMaterials = new List<Material> { rawMaterial, finishedGood };

        _repository.ListBalancesAsync(null, InventorySourceType.Production, cancellationToken).Returns(activeBalances);
        _materialRepository.ListAllAsync(cancellationToken).Returns(allMaterials);

        // Act
        var result = await _sut.ExecuteAsync(null, InventorySourceType.Production, cancellationToken);

        // Assert
        // Should only return ROL-01 (FinishedGood) and exclude MAT-212 (RawMaterial)
        result.Should().HaveCount(1);
        result.Single().MaterialCode.Should().Be("ROL-01");
    }
}
