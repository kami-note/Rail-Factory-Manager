using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class PostgresSupplyChainRepository(SupplyChainDbContext dbContext) : ISupplyChainRepository
{
    public Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, CancellationToken cancellationToken)
        => dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierId, cancellationToken);

    public Task<Supplier?> GetSupplierByFiscalIdAsync(string fiscalId, CancellationToken cancellationToken)
        => dbContext.Suppliers.FirstOrDefaultAsync(x => x.FiscalId == fiscalId, cancellationToken);

    public Task AddSupplierAsync(Supplier supplier, CancellationToken cancellationToken)
        => dbContext.Suppliers.AddAsync(supplier, cancellationToken).AsTask();

    public Task AddReceiptAsync(MaterialReceipt receipt, CancellationToken cancellationToken)
        => dbContext.Receipts.AddAsync(receipt, cancellationToken).AsTask();

    public Task<List<MaterialReceipt>> ListReceiptsAsync(string tenantCode, CancellationToken cancellationToken)
        => dbContext.Receipts
            .AsNoTracking()
            .Where(x => x.TenantCode == tenantCode)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddAuditEntryAsync(SupplyAuditEntry entry, CancellationToken cancellationToken)
        => dbContext.AuditEntries.AddAsync(entry, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
