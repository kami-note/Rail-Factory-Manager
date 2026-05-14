using FluentAssertions;
using NSubstitute;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Materials;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Tests;

public class MergeMaterialUseCaseTests
{
    private readonly IMaterialRepository _materialRepository = Substitute.For<IMaterialRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly MergeMaterialUseCase _sut;

    public MergeMaterialUseCaseTests()
    {
        _sut = new MergeMaterialUseCase(_materialRepository, _inventoryRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTransferBalancesAndMarkObsolete()
    {
        // Arrange
        var obsoleteCode = MaterialCode.From("OBS-001");
        var officialCode = MaterialCode.From("OFF-001");
        var actor = EmailAddress.From("actor@test.com");
        var command = new MergeMaterialCommand(obsoleteCode, officialCode, actor);

        var obsoleteMaterial = CreateMaterial(obsoleteCode);
        var officialMaterial = CreateMaterial(officialCode);

        _materialRepository.GetByCodesAsync(
            Arg.Is<IEnumerable<string>>(x => x.Contains(obsoleteCode.Value) && x.Contains(officialCode.Value)),
            Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Material>
            {
                { obsoleteCode.Value, obsoleteMaterial },
                { officialCode.Value, officialMaterial }
            });

        var balance = InventoryBalance.CreatePending(
            obsoleteCode.Value,
            "UN",
            10,
            Guid.NewGuid(),
            "REF-001",
            "LOT-123",
            null,
            InventorySourceType.Purchase,
            null);
        
        // Confirm it so it's not pending (the use case expects to confirm it to 0)
        // Wait, the use case calls balance.Confirm(0, ...). 
        // My domain logic says: if (Status != InventoryBalanceStatus.Pending) throw ...
        // Let's check the use case again.
        
        _inventoryRepository.GetBalancesByMaterialCodeAsync(obsoleteCode.Value, Arg.Any<CancellationToken>())
            .Returns(new List<InventoryBalance> { balance });

        // Act
        await _sut.ExecuteAsync(command, CancellationToken.None);

        // Assert
        obsoleteMaterial.Status.Should().Be(MaterialStatus.Obsolete);
        obsoleteMaterial.ReplacedBy.Should().Be(officialCode);
        
        balance.Quantity.Should().Be(0);
        balance.Status.Should().Be(InventoryBalanceStatus.Blocked);

        await _inventoryRepository.Received(1).AddBalanceAsync(
            Arg.Is<InventoryBalance>(b => b.MaterialCode == officialCode.Value && b.Quantity == 10),
            Arg.Any<CancellationToken>());

        await _materialRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _inventoryRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenObsoleteMaterialNotFound()
    {
        // Arrange
        var obsoleteCode = MaterialCode.From("OBS-001");
        var officialCode = MaterialCode.From("OFF-001");
        var command = new MergeMaterialCommand(obsoleteCode, officialCode, EmailAddress.From("a@a.com"));

        _materialRepository.GetByCodesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Material>());

        // Act & Assert
        await _sut.Invoking(x => x.ExecuteAsync(command))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Obsolete material*not found*");
    }

    private static Material CreateMaterial(MaterialCode code)
    {
        return Material.Create(
            code.Value,
            "Test Material",
            "Description",
            MaterialCategory.RawMaterial,
            ProcurementType.Buy,
            EmailAddress.From("creator@test.com"),
            "UN"
        );
    }
}
