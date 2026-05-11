using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainDbContext(DbContextOptions<SupplyChainDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierMaterialMapping> SupplierMaterialMappings => Set<SupplierMaterialMapping>();
    public DbSet<MaterialReceipt> Receipts => Set<MaterialReceipt>();
    public DbSet<MaterialReceiptItem> ReceiptItems => Set<MaterialReceiptItem>();
    public DbSet<SupplyAuditEntry> AuditEntries => Set<SupplyAuditEntry>();
    public DbSet<SupplyOutboxMessage> OutboxMessages => Set<SupplyOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierMaterialMapping>(entity =>
        {
            entity.ToTable("supplier_material_mappings");
            entity.HasKey(x => x.Id);
            
            entity.Property(x => x.SupplierFiscalId)
                .HasConversion(v => v.Value, v => FiscalId.From(v))
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.SupplierProductCode).HasMaxLength(100).IsRequired();
            
            entity.Property(x => x.InternalMaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.InternalUnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.SupplierUnit).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ConversionFactor).HasColumnType("numeric(18,4)").IsRequired();
            
            // ELITE FIX: Defensive conversion for EmailAddress to handle legacy/empty data.
            const string DefaultSystemEmail = "system@railfactory.local";

            entity.Property(x => x.CreatedBy)
                .HasConversion(
                    v => v.Value, 
                    v => string.IsNullOrWhiteSpace(v) ? EmailAddress.From(DefaultSystemEmail) : EmailAddress.From(v))
                .HasMaxLength(256)
                .IsRequired();
                
            entity.Property(x => x.LastModifiedBy)
                .HasConversion(
                    v => v.Value, 
                    v => string.IsNullOrWhiteSpace(v) ? EmailAddress.From(DefaultSystemEmail) : EmailAddress.From(v))
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(x => new { x.SupplierFiscalId, x.SupplierProductCode }).IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(x => x.Id);
            
            entity.Property(x => x.FiscalId)
                .HasConversion(v => v.Value, v => FiscalId.From(v))
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.FiscalId).IsUnique();
        });

        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.ToTable("material_receipts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReceiptNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccessKey).HasMaxLength(44);
            entity.Property(x => x.TotalValue).HasColumnType("numeric(18,2)");
            entity.Property(x => x.RawXml).HasColumnType("text");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ReceiptNumber).IsUnique();
        });

        modelBuilder.Entity<MaterialReceiptItem>(entity =>
        {
            entity.ToTable("material_receipt_items");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.SupplierProductCode)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.SupplierQuantity)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.SupplierUnitOfMeasure)
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(x => x.InternalMaterialCode)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => string.IsNullOrWhiteSpace(v) ? null : MaterialCode.From(v))
                .HasMaxLength(64);

            entity.Property(x => x.AssociationStatus)
                .HasConversion<string>()
                .HasMaxLength(24)
                .IsRequired();

            entity.Property(x => x.AssociationConversionFactor)
                .HasColumnType("numeric(18,4)");

            entity.Property(x => x.AssociationReason)
                .HasMaxLength(512);

            entity.Property(x => x.AssociationUpdatedBy)
                .HasMaxLength(256);

            entity.Property(x => x.ExpectedQuantity).HasColumnType("numeric(18,4)");
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
            entity.Property(x => x.OriginalDescription).HasMaxLength(256);
            entity.Property(x => x.CountedQuantity).HasColumnType("numeric(18,4)");
            entity.Property(x => x.ConfirmedLotNumber).HasMaxLength(64);
            entity.Property(x => x.ConfirmedExpirationDate);
            entity.HasIndex(x => new { x.ReceiptId, x.MaterialCode });
        });

        modelBuilder.Entity<SupplyAuditEntry>(entity =>
        {
            entity.ToTable("supply_audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UserIdentifier).HasMaxLength(256).IsRequired();
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<SupplyOutboxMessage>(entity =>
        {
            entity.ToTable("supply_outbox_messages");
            entity.HasKey(x => x.Id);
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
