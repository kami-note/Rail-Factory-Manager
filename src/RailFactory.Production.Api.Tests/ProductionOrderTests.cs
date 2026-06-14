using FluentAssertions;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="ProductionOrder"/> aggregate root.
/// </summary>
public class ProductionOrderTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var number = "OP-2026-0001";
        var product = "PROD-123";
        var bomId = Guid.NewGuid();
        var workCenterId = Guid.NewGuid();
        var qty = 100.5m;

        // Act
        var order = ProductionOrder.Create(number, product, bomId, workCenterId, qty);

        // Assert
        order.Id.Should().NotBeEmpty();
        order.OrderNumber.Should().Be("OP-2026-0001"); // trimmed and uppercased
        order.ProductCode.Value.Should().Be("PROD-123");
        order.BomId.Should().Be(bomId);
        order.WorkCenterId.Should().Be(workCenterId);
        order.PlannedQuantity.Should().Be(qty);
        order.Status.Should().Be(ProductionOrderStatus.Draft);
    }

    [Theory]
    [InlineData("", "PROD-123")]
    [InlineData("OP-1", "")]
    [InlineData("   ", "PROD-123")]
    [InlineData("OP-1", "   ")]
    public void Create_WithInvalidNumberOrProduct_ShouldThrowArgumentException(string number, string product)
    {
        // Act
        Action act = () => ProductionOrder.Create(number, product, Guid.NewGuid(), Guid.NewGuid(), 10m);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyBomId_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => ProductionOrder.Create("OP-1", "PROD-123", Guid.Empty, Guid.NewGuid(), 10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*A valid BOM must be specified*");
    }

    [Fact]
    public void Create_WithEmptyWorkCenterId_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.Empty, 10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*A valid Work Center must be specified*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.5)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentException(decimal invalidQty)
    {
        // Act
        Action act = () => ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Planned quantity must be greater than zero*");
    }

    [Fact]
    public void Release_WhenDraft_ShouldTransitionToReleased()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);

        // Act
        order.Release();

        // Assert
        order.Status.Should().Be(ProductionOrderStatus.Released);
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();

        // Act
        Action act = () => order.Release();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only Draft orders can be released*");
    }

    [Fact]
    public void StartExecution_WhenReleased_ShouldTransitionToInExecution()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();

        // Act
        order.StartExecution();

        // Assert
        order.Status.Should().Be(ProductionOrderStatus.InExecution);
    }

    [Fact]
    public void StartExecution_WhenDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);

        // Act
        Action act = () => order.StartExecution();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only Released orders can be started*");
    }

    [Fact]
    public void Complete_WhenInExecutionAndInspectionPassed_ShouldTransitionToCompleted()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();
        order.StartExecution();

        // Act
        order.Complete(inspectionPassed: true);

        // Assert
        order.Status.Should().Be(ProductionOrderStatus.Completed);
    }

    [Fact]
    public void Complete_WhenInExecutionButInspectionFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();
        order.StartExecution();

        // Act
        Action act = () => order.Complete(inspectionPassed: false);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*quality inspection has not been approved*");
    }

    [Fact]
    public void Complete_WhenDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);

        // Act
        Action act = () => order.Complete(inspectionPassed: true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only InExecution orders can be completed*");
    }

    [Fact]
    public void Cancel_WhenDraft_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(ProductionOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenReleased_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(ProductionOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenInExecution_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Release();
        order.StartExecution();

        // Act
        Action act = () => order.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a Production Order in status 'InExecution'*");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ProductionOrder.Create("OP-1", "PROD-123", Guid.NewGuid(), Guid.NewGuid(), 10m);
        order.Cancel();

        // Act
        Action act = () => order.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already cancelled*");
    }
}
