using Microsoft.Extensions.Logging.Abstractions;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;
using RailFactory.SupplyChain.Api.Domain;
using RailFactory.SupplyChain.Api.Infrastructure.Integration;
using Xunit;

namespace RailFactory.SupplyChain.Api.Tests;

public sealed class ImportXmlReceiptBatchTests
{
    [Fact]
    public async Task ExecuteAsync_imports_all_valid_documents()
    {
        var repository = new FakeSupplyChainRepository();
        var useCase = CreateUseCase(repository);

        var imported = await useCase.ExecuteAsync(
            "tester",
            [
                new ImportXmlReceiptBatchDocument("one.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-001")),
                new ImportXmlReceiptBatchDocument("two.xml", BuildNfe("35260599999090910270550010000000025180051273", "MAT-002"))
            ],
            "corr-1",
            CancellationToken.None);

        Assert.Equal(2, imported.Count);
        Assert.Equal(2, repository.Receipts.Count);
        Assert.Equal(2, repository.OutboxMessages.Count);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_invalid_batch_without_writes()
    {
        var repository = new FakeSupplyChainRepository();
        var useCase = CreateUseCase(repository);

        var error = await Assert.ThrowsAsync<ImportXmlReceiptBatchValidationException>(() => useCase.ExecuteAsync(
            "tester",
            [
                new ImportXmlReceiptBatchDocument("one.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-001")),
                new ImportXmlReceiptBatchDocument("bad.xml", "<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" />")
            ],
            "corr-1",
            CancellationToken.None));

        Assert.Contains(error.Errors, x => x.FileName == "bad.xml");
        Assert.Empty(repository.Suppliers);
        Assert.Empty(repository.Receipts);
        Assert.Empty(repository.OutboxMessages);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_duplicate_access_key_without_writes()
    {
        var repository = new FakeSupplyChainRepository();
        var useCase = CreateUseCase(repository);

        var error = await Assert.ThrowsAsync<ImportXmlReceiptBatchValidationException>(() => useCase.ExecuteAsync(
            "tester",
            [
                new ImportXmlReceiptBatchDocument("one.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-001")),
                new ImportXmlReceiptBatchDocument("two.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-002"))
            ],
            "corr-1",
            CancellationToken.None));

        Assert.Equal(2, error.Errors.Count);
        Assert.Empty(repository.Suppliers);
        Assert.Empty(repository.Receipts);
        Assert.Empty(repository.OutboxMessages);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_existing_access_key_without_writes()
    {
        var repository = new FakeSupplyChainRepository();
        var existingSupplier = Supplier.Create("99999090910270", "Existing supplier");
        repository.Suppliers.Add(existingSupplier);
        repository.Receipts.Add(MaterialReceipt.Create(
            "NFE-35260599999090910270550010000000015180051273",
            existingSupplier.Id,
            "35260599999090910270550010000000015180051273",
            null,
            null,
            null,
            new DateOnly(2026, 5, 3)));

        var useCase = CreateUseCase(repository);

        var error = await Assert.ThrowsAsync<ImportXmlReceiptBatchValidationException>(() => useCase.ExecuteAsync(
            "tester",
            [
                new ImportXmlReceiptBatchDocument("one.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-001"))
            ],
            "corr-1",
            CancellationToken.None));

        Assert.Contains(error.Errors, x => x.FileName == "one.xml" && x.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        Assert.Single(repository.Receipts);
        Assert.Empty(repository.OutboxMessages);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task ExecuteAsync_maps_save_conflict_to_batch_validation()
    {
        var repository = new FakeSupplyChainRepository
        {
            ReceiptConflictOnSave = "NFE-35260599999090910270550010000000015180051273"
        };
        var useCase = CreateUseCase(repository);

        var error = await Assert.ThrowsAsync<ImportXmlReceiptBatchValidationException>(() => useCase.ExecuteAsync(
            "tester",
            [
                new ImportXmlReceiptBatchDocument("one.xml", BuildNfe("35260599999090910270550010000000015180051273", "MAT-001"))
            ],
            "corr-1",
            CancellationToken.None));

        Assert.Contains(error.Errors, x => x.FileName == "request" && x.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, repository.SaveChangesCount);
    }


    private static ImportXmlReceiptBatch CreateUseCase(FakeSupplyChainRepository repository) =>
        new(
            new XmlReceiptBatchParser(new BasicXmlNfeProvider()),
            repository,
            new MaterialReceiptWriter(repository, repository),
            new ImmediateTransactionRunner());

    private static string BuildNfe(string accessKey, string materialCode) =>
        NfeXmlSamples.BuildOfficialNfe(accessKey, materialCode);

    private sealed class FakeSupplyChainRepository : ISupplyChainRepository, ISupplyOutbox
    {
        public List<Supplier> Suppliers { get; } = [];
        public List<MaterialReceipt> Receipts { get; } = [];
        public List<object> OutboxMessages { get; } = [];
        public int SaveChangesCount { get; private set; }
        public string? ReceiptConflictOnSave { get; init; }

        public Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, CancellationToken cancellationToken) =>
            Task.FromResult(Suppliers.FirstOrDefault(x => x.Id == supplierId));

        public Task<Supplier?> GetSupplierByFiscalIdAsync(string fiscalId, CancellationToken cancellationToken) =>
            Task.FromResult(Suppliers.FirstOrDefault(x => x.FiscalId == fiscalId));

        public Task<MaterialReceipt?> GetReceiptByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken) =>
            Task.FromResult(Receipts.FirstOrDefault(x => x.ReceiptNumber == receiptNumber));

        public Task<MaterialReceipt?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Receipts.FirstOrDefault(x => x.Id == id));

        public Task AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken)
        {
            Suppliers.Add(supplier);
            return Task.CompletedTask;
        }

        public Task AddReceiptAsync(MaterialReceipt receipt, CancellationToken cancellationToken)
        {
            Receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task<List<MaterialReceipt>> ListReceiptsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Receipts.ToList());

        public Task AddAuditEntryAsync(SupplyAuditEntry entry, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<List<SupplyAuditEntry>> GetAuditEntriesByReceiptIdAsync(Guid receiptId, CancellationToken cancellationToken) =>
            Task.FromResult(new List<SupplyAuditEntry>());

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            if (ReceiptConflictOnSave is not null)
            {
                throw new ReceiptAlreadyExistsException(ReceiptConflictOnSave);
            }

            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string eventType, object payload, string correlationId, CancellationToken cancellationToken)
        {
            OutboxMessages.Add(payload);
            return Task.CompletedTask;
        }
    }

    private sealed class ImmediateTransactionRunner : ISupplyChainTransactionRunner
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }
}
