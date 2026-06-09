using FluentAssertions;
using NSubstitute;
using RailFactory.BuildingBlocks.Domain;
using RailFactory.Production.Api.Application.Boms;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Application tests for the <see cref="CloneBomVersion"/> use case.
/// </summary>
public class CloneBomVersionTests
{
    private readonly IBomRepository _repository = Substitute.For<IBomRepository>();
    private readonly CloneBomVersion _sut;

    public CloneBomVersionTests()
    {
        _sut = new CloneBomVersion(_repository);
    }

    /// <summary>
    /// Verifies that CloneBomVersion successfully clones a BOM and its items, generating the next version.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCloneBomAndItems_WithNextVersionNumber()
    {
        // Given
        var originalId = Guid.NewGuid();
        var original = BillOfMaterials.Create("MAT-ABC", 1, 10.0m);
        original.AddItem("COMP-1", 5m, "UN");
        original.AddItem("COMP-2", 3m, "KG");

        // Reflecting the Guid PK using base Entity type to resolve private set
        var idProperty = typeof(Entity<Guid>).GetProperty("Id");
        idProperty?.GetSetMethod(nonPublic: true)?.Invoke(original, [originalId]);

        _repository.GetLatestVersionNumberAsync("MAT-ABC", Arg.Any<CancellationToken>())
            .Returns(1);

        BillOfMaterials? savedBom = null;
        await _repository.AddAsync(Arg.Do<BillOfMaterials>(b => savedBom = b), Arg.Any<CancellationToken>());

        var clonedItems = new List<BomItem>();
        await _repository.AddItemDirectAsync(
            Arg.Any<Guid>(),
            Arg.Do<BomItem>(i => clonedItems.Add(i)),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());

        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(x => {
                var id = (Guid)x[0];
                if (id == originalId)
                {
                    return original;
                }
                if (savedBom != null && savedBom.Id == id)
                {
                    // Mock reloading with the added items
                    savedBom.GetType()
                        .GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(savedBom, new List<BomItem>(clonedItems));
                    return savedBom;
                }
                return null;
            });

        // When
        var result = await _sut.ExecuteAsync(originalId, CancellationToken.None);

        // Then
        result.Should().NotBeNull();
        result.ProductCode.Value.Should().Be("MAT-ABC");
        result.Version.Should().Be(2);
        result.Status.Should().Be(BomStatus.Draft);
        result.BatchSize.Should().Be(10.0m);

        await _repository.Received(1).AddAsync(Arg.Any<BillOfMaterials>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.Received(2).AddItemDirectAsync(Arg.Any<Guid>(), Arg.Any<BomItem>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        
        clonedItems.Should().HaveCount(2);
        clonedItems.Should().ContainSingle(i => i.MaterialCode.Value == "COMP-1" && i.Quantity == 5m && i.UnitOfMeasure == "UN");
        clonedItems.Should().ContainSingle(i => i.MaterialCode.Value == "COMP-2" && i.Quantity == 3m && i.UnitOfMeasure == "KG");
    }

    /// <summary>
    /// Verifies that CloneBomVersion throws a KeyNotFoundException when original BOM doesn't exist.
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
            .WithMessage("*not found*");
    }
}
