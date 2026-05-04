using Microsoft.EntityFrameworkCore;
using Npgsql;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class PostgresSupplyChainRepository(SupplyChainDbContext dbContext) : ISupplyChainRepository
{
    public Task<Supplier?> GetSupplierByIdAsync(Guid supplierId, CancellationToken cancellationToken)
        => dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierId, cancellationToken);

    public Task<Supplier?> GetSupplierByFiscalIdAsync(string fiscalId, CancellationToken cancellationToken)
        => dbContext.Suppliers.FirstOrDefaultAsync(x => x.FiscalId == fiscalId, cancellationToken);

    public Task<MaterialReceipt?> GetReceiptByReceiptNumberAsync(string tenantCode, string receiptNumber, CancellationToken cancellationToken)
        => dbContext.Receipts.FirstOrDefaultAsync(x => x.TenantCode == tenantCode && x.ReceiptNumber == receiptNumber, cancellationToken);

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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (TryGetReceiptUniqueConflict(ex, out var receiptNumber))
        {
            throw new ReceiptAlreadyExistsException(receiptNumber);
        }
    }

    private static bool TryGetReceiptUniqueConflict(DbUpdateException exception, out string receiptNumber)
    {
        receiptNumber = string.Empty;
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_material_receipts_TenantCode_ReceiptNumber" })
        {
            return false;
        }

        var receipt = exception.Entries
            .Select(x => x.Entity)
            .OfType<MaterialReceipt>()
            .FirstOrDefault();

        receiptNumber = receipt?.ReceiptNumber ?? "unknown";
        return true;
    }
}
