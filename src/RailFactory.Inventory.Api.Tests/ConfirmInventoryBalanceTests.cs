using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Tests;

/// <summary>
/// Unit and Application tests for the Inventory Balance confirmation logic.
/// </summary>
public class ConfirmInventoryBalanceTests
{
    private readonly IInventoryRepository _repository = Substitute.For<IInventoryRepository>();
    private readonly ConfirmInventoryBalance _sut;

    public ConfirmInventoryBalanceTests()
    {
        _sut = new ConfirmInventoryBalance(_repository);
    }

    #region Domain Method Tests: InventoryBalance.Confirm

    /// <summary>
    /// Verifies that a pending balance can be successfully confirmed and becomes 'Available'.
    /// </summary>
    [Fact]
    public void Confirm_WhenPending_ShouldTransitionToAvailable_AndUpdateDetails()
    {
        // Given
        var balance = CreatePendingBalance(10, "UN");
        var newQuantity = 12m;
        var newLot = "LOT-ABC";
        var newExpiry = DateTimeOffset.UtcNow.AddDays(30);

        // When
        balance.Confirm(newQuantity, newLot, newExpiry, isApproved: true);

        // Then
        balance.Status.Should().Be(InventoryBalanceStatus.Available);
        balance.Quantity.Should().Be(newQuantity);
        balance.LotNumber.Should().Be(newLot);
        balance.ExpirationDate.Should().Be(newExpiry);
    }

    /// <summary>
    /// Verifies that a pending balance becomes 'Blocked' when not approved.
    /// </summary>
    [Fact]
    public void Confirm_WhenPending_ShouldTransitionToBlocked_WhenNotApproved()
    {
        // Given
        var balance = CreatePendingBalance(10, "UN");

        // When
        balance.Confirm(10, "LOT-123", null, isApproved: false);

        // Then
        balance.Status.Should().Be(InventoryBalanceStatus.Blocked);
    }

    /// <summary>
    /// Verifies the Status Guard: confirming a balance that is already Available must fail.
    /// </summary>
    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrowInvalidOperationException()
    {
        // Given
        var balance = CreatePendingBalance(10, "UN");
        balance.Confirm(10, null, null, isApproved: true);

        // When
        Action act = () => balance.Confirm(10, null, null, isApproved: true);

        // Then
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only 'Pending' balances can be confirmed*");
    }

    /// <summary>
    /// Verifies that negative quantities are rejected.
    /// </summary>
    [Fact]
    public void Confirm_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Given
        var balance = CreatePendingBalance(10, "UN");

        // When
        Action act = () => balance.Confirm(-1, null, null, isApproved: true);

        // Then
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    #endregion

    #region Application Use Case Tests: ConfirmInventoryBalance

    /// <summary>
    /// Verifies idempotency: if the integration message was already processed, do nothing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenMessageAlreadyProcessed_ShouldReturnFalse()
    {
        // Given
        var input = CreateInput();
        _repository.IntegrationMessageProcessedAsync(input.EventId, Arg.Any<CancellationToken>())
            .Returns(true);

        // When
        var result = await _sut.ExecuteAsync(input, CancellationToken.None);

        // Then
        result.Should().BeFalse();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the use case fails if the balance does not exist.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenBalanceNotFound_ShouldThrowInvalidOperationException()
    {
        // Given
        var input = CreateInput();
        _repository.IntegrationMessageProcessedAsync(input.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.GetBalanceBySourceReferenceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InventoryBalance?)null);

        // When
        Func<Task> act = () => _sut.ExecuteAsync(input, CancellationToken.None);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    /// <summary>
    /// Verifies a successful confirmation flow including persistence and auditing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenValid_ShouldConfirmBalanceAndPersistState()
    {
        // Given
        var initialQuantity = 10m;
        var countedQuantity = 15m;
        var balance = CreatePendingBalance(initialQuantity, "UN");
        var input = CreateInput() with { CountedQuantity = countedQuantity, IsApproved = true };

        _repository.IntegrationMessageProcessedAsync(input.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.GetBalanceBySourceReferenceAsync($"{input.ReceiptId}:{input.ReceiptItemId}", Arg.Any<CancellationToken>())
            .Returns(balance);

        // When
        var result = await _sut.ExecuteAsync(input, CancellationToken.None);

        // Then
        result.Should().BeTrue();
        balance.Status.Should().Be(InventoryBalanceStatus.Available);
        balance.Quantity.Should().Be(countedQuantity);

        await _repository.Received(1).AddIntegrationMessageAsync(
            Arg.Is<InventoryIntegrationMessage>(m => m.EventId == input.EventId),
            Arg.Any<CancellationToken>());

        await _repository.Received(1).AddLedgerEntryAsync(
            Arg.Is<InventoryLedgerEntry>(e => e.BalanceId == balance.Id && e.Operation == "balance_confirmed"),
            Arg.Any<CancellationToken>());

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that the ledger entry records the correct quantity delta.
    /// This test targets a potential bug where delta is calculated after the balance is updated.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldRecordCorrectLedgerDelta()
    {
        // Given
        var initialQuantity = 100m;
        var countedQuantity = 120m;
        var balance = CreatePendingBalance(initialQuantity, "UN");
        var input = CreateInput() with { CountedQuantity = countedQuantity };

        _repository.IntegrationMessageProcessedAsync(input.EventId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.GetBalanceBySourceReferenceAsync($"{input.ReceiptId}:{input.ReceiptItemId}", Arg.Any<CancellationToken>())
            .Returns(balance);

        // When
        await _sut.ExecuteAsync(input, CancellationToken.None);

        // Then
        // The expected delta should be +20 (120 - 100)
        await _repository.Received(1).AddLedgerEntryAsync(
            Arg.Is<InventoryLedgerEntry>(e => e.QuantityDelta == 20m),
            Arg.Any<CancellationToken>());
    }

    #endregion

    private static InventoryBalance CreatePendingBalance(decimal quantity, string uom)
    {
        return InventoryBalance.CreatePending(
            "MAT-001",
            uom,
            quantity,
            Guid.NewGuid(),
            "REF-001",
            null,
            null,
            InventorySourceType.Purchase,
            null);
    }

    private static ConfirmInventoryBalanceInput CreateInput()
    {
        return new ConfirmInventoryBalanceInput(
            Guid.NewGuid(),
            "RECEIPT_CONFIRMED",
            "CORR-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Approved",
            true,
            10m,
            "LOT-123",
            null);
    }
}
