using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;
using RailFactory.SupplyChain.Api.Domain;
using Xunit;

namespace RailFactory.SupplyChain.Api.Tests;

/// <summary>
/// Verifies the Blind Conference closing logic and status transitions.
/// </summary>
public sealed class CloseMaterialReceiptConferenceTests
{
    #region Domain Tests

    /// <summary>
    /// GIVEN a receipt in 'InConference' status
    /// WHEN all items are counted with matching quantities
    /// THEN the receipt status should become 'Approved'.
    /// </summary>
    [Fact]
    public void CloseConference_with_matching_quantities_sets_status_to_Approved()
    {
        // Arrange
        var receipt = CreateInConferenceReceipt();
        var item1 = receipt.Items[0];
        var item2 = receipt.Items[1];

        var results = new List<CountedItemResult>
        {
            new(item1.Id, item1.ExpectedQuantity, "LOT-123", DateTimeOffset.UtcNow.AddMonths(6)),
            new(item2.Id, item2.ExpectedQuantity, "LOT-456", DateTimeOffset.UtcNow.AddMonths(12))
        };

        // Act
        receipt.CloseConference(results);

        // Assert
        Assert.Equal(MaterialReceiptStatus.Approved, receipt.Status);
        Assert.False(item1.HasDivergence);
        Assert.False(item2.HasDivergence);
    }

    /// <summary>
    /// GIVEN a receipt in 'InConference' status
    /// WHEN at least one item has a quantity divergence
    /// THEN the receipt status should become 'Divergent'.
    /// </summary>
    [Fact]
    public void CloseConference_with_divergent_quantity_sets_status_to_Divergent()
    {
        // Arrange
        var receipt = CreateInConferenceReceipt();
        var item1 = receipt.Items[0];
        var item2 = receipt.Items[1];

        var results = new List<CountedItemResult>
        {
            new(item1.Id, item1.ExpectedQuantity - 1, "LOT-123", DateTimeOffset.UtcNow.AddMonths(6)), // Divergent
            new(item2.Id, item2.ExpectedQuantity, "LOT-456", DateTimeOffset.UtcNow.AddMonths(12))
        };

        // Act
        receipt.CloseConference(results);

        // Assert
        Assert.Equal(MaterialReceiptStatus.Divergent, receipt.Status);
        Assert.True(item1.HasDivergence);
        Assert.False(item2.HasDivergence);
    }

    /// <summary>
    /// GIVEN a receipt NOT in 'InConference' status
    /// WHEN attempting to close the conference
    /// THEN it should throw InvalidOperationException (Status Guard).
    /// </summary>
    [Fact]
    public void CloseConference_throws_when_status_is_not_InConference()
    {
        // Arrange
        var receipt = MaterialReceipt.Create("REC-001", Guid.NewGuid(), "DOC-001", null, 100, null, DateOnly.FromDateTime(DateTime.Today));
        // Status is 'Registered'

        var results = new List<CountedItemResult>();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => receipt.CloseConference(results));
        Assert.Contains("Only 'InConference' receipts can be closed", ex.Message);
    }

    /// <summary>
    /// GIVEN a receipt in 'InConference' status
    /// WHEN provided results for an item that does not belong to the receipt
    /// THEN it should throw InvalidOperationException.
    /// </summary>
    [Fact]
    public void CloseConference_throws_when_item_id_not_found()
    {
        // Arrange
        var receipt = CreateInConferenceReceipt();
        var results = new List<CountedItemResult>
        {
            new(Guid.NewGuid(), 10, null, null) // Random ID
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => receipt.CloseConference(results));
    }

    /// <summary>
    /// GIVEN a receipt in 'InConference' status
    /// WHEN provided a negative quantity for an item
    /// THEN it should throw ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void CloseConference_throws_when_quantity_is_negative()
    {
        // Arrange
        var receipt = CreateInConferenceReceipt();
        var item1 = receipt.Items[0];
        var results = new List<CountedItemResult>
        {
            new(item1.Id, -1, null, null)
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.CloseConference(results));
    }

    #endregion

    #region Application Tests

    /// <summary>
    /// GIVEN a valid close conference command
    /// WHEN executed
    /// THEN it should save the receipt, create an audit entry, and enqueue integration events.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_persists_changes_and_enqueues_outbox_events()
    {
        // Arrange
        var repository = new FakeSupplyChainRepository();
        var outbox = new FakeSupplyOutbox();
        var transactionRunner = new ImmediateTransactionRunner();
        var useCase = new CloseMaterialReceiptConference(repository, transactionRunner, outbox);

        var receipt = CreateInConferenceReceipt();
        repository.Receipts.Add(receipt);

        var item1 = receipt.Items[0];
        var item2 = receipt.Items[1];

        var inputs = new List<CloseConferenceItemInput>
        {
            new(item1.Id, item1.ExpectedQuantity, "LOT-1", null),
            new(item2.Id, item2.ExpectedQuantity + 5, "LOT-2", null) // Divergent
        };

        // Act
        await useCase.ExecuteAsync(receipt.Id, "operator@factory.com", inputs, "corr-123", CancellationToken.None);

        // Assert
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.Equal(MaterialReceiptStatus.Divergent, receipt.Status);

        // Verify Audit
        var audit = repository.AuditEntries.Single();
        Assert.Equal("conference_closed", audit.Action);
        Assert.Equal("operator@factory.com", audit.UserIdentifier);
        Assert.Contains(receipt.Id.ToString(), audit.MetadataJson);

        // Verify Outbox Events
        Assert.Equal(2, outbox.EnqueuedMessages.Count);

        var event1 = (ReceiptItemConferredIntegrationEvent)outbox.EnqueuedMessages[0].Payload;
        Assert.Equal(item1.Id, event1.ReceiptItemId);
        Assert.True(event1.IsItemApproved); // item1 matched
        Assert.Equal(item1.ExpectedQuantity, event1.CountedQuantity);

        var event2 = (ReceiptItemConferredIntegrationEvent)outbox.EnqueuedMessages[1].Payload;
        Assert.Equal(item2.Id, event2.ReceiptItemId);
        Assert.False(event2.IsItemApproved); // item2 diverged
        Assert.Equal(item2.ExpectedQuantity + 5, event2.CountedQuantity);
    }

    #endregion

    #region Helpers

    private static MaterialReceipt CreateInConferenceReceipt()
    {
        var receipt = MaterialReceipt.Create("REC-001", Guid.NewGuid(), "DOC-001", null, 100, null, DateOnly.FromDateTime(DateTime.Today));
        receipt.AddItem("MAT-A", "UN", 10);
        receipt.AddItem("MAT-B", "KG", 50);
        receipt.StartConference();
        return receipt;
    }

    private sealed class FakeSupplyChainRepository : ISupplyChainRepository
    {
        public List<MaterialReceipt> Receipts { get; } = [];
        public List<SupplyAuditEntry> AuditEntries { get; } = [];
        public int SaveChangesCount { get; private set; }

        public Task<MaterialReceipt?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Receipts.FirstOrDefault(x => x.Id == id));

        public Task AddAuditEntryAsync(SupplyAuditEntry entry, CancellationToken cancellationToken)
        {
            AuditEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }

        // Not used in this test
        public Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Supplier?> GetSupplierByFiscalIdAsync(string fiscalId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<MaterialReceipt?> GetReceiptByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddReceiptAsync(MaterialReceipt receipt, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<MaterialReceipt>> ListReceiptsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<SupplyAuditEntry>> GetAuditEntriesByReceiptIdAsync(Guid receiptId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SupplierMaterialMapping?> GetSupplierMaterialMappingAsync(string supplierFiscalId, string supplierProductCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddSupplierMaterialMappingAsync(SupplierMaterialMapping mapping, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeSupplyOutbox : ISupplyOutbox
    {
        public List<(string EventType, object Payload)> EnqueuedMessages { get; } = [];

        public Task EnqueueAsync(string eventType, object payload, string correlationId, CancellationToken cancellationToken)
        {
            EnqueuedMessages.Add((eventType, payload));
            return Task.CompletedTask;
        }
    }

    private sealed class ImmediateTransactionRunner : ISupplyChainTransactionRunner
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    #endregion
}
