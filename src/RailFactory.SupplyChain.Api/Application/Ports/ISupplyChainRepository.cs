using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyChainRepository
{
    Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, CancellationToken cancellationToken);
    Task<Supplier?> GetSupplierByFiscalIdAsync(string fiscalId, CancellationToken cancellationToken);
    Task<MaterialReceipt?> GetReceiptByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken);
    Task<MaterialReceipt?> GetReceiptByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken);
    Task AddReceiptAsync(MaterialReceipt receipt, CancellationToken cancellationToken);
    Task<List<MaterialReceipt>> ListReceiptsAsync(CancellationToken cancellationToken);
    Task AddAuditEntryAsync(SupplyAuditEntry entry, CancellationToken cancellationToken);
    Task<List<SupplyAuditEntry>> GetAuditEntriesByReceiptIdAsync(Guid receiptId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
