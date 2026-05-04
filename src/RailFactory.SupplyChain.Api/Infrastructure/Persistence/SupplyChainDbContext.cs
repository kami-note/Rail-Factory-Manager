using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainDbContext(DbContextOptions<SupplyChainDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<MaterialReceipt> Receipts => Set<MaterialReceipt>();
    public DbSet<MaterialReceiptItem> ReceiptItems => Set<MaterialReceiptItem>();
    public DbSet<SupplyAuditEntry> AuditEntries => Set<SupplyAuditEntry>();
    public DbSet<SupplyOutboxMessage> OutboxMessages => Set<SupplyOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FiscalId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.FiscalId).IsUnique();
        });

        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.ToTable("material_receipts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReceiptNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TenantCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TenantCode, x.ReceiptNumber }).IsUnique();
        });

        modelBuilder.Entity<MaterialReceiptItem>(entity =>
        {
            entity.ToTable("material_receipt_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExpectedQuantity).HasColumnType("numeric(18,4)");
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.HasIndex(x => new { x.ReceiptId, x.MaterialCode });
        });

        modelBuilder.Entity<SupplyAuditEntry>(entity =>
        {
            entity.ToTable("supply_audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UserIdentifier).HasMaxLength(256).IsRequired();
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<SupplyOutboxMessage>(entity =>
        {
            entity.ToTable("supply_outbox_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.DispatchedAt, x.CreatedAt });
            entity.HasIndex(x => new { x.Status, x.CreatedAt });
        });
    }
}
