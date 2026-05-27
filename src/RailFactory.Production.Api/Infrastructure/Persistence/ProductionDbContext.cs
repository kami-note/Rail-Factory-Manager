using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class ProductionDbContext(DbContextOptions<ProductionDbContext> options) : DbContext(options)
{
    public DbSet<WorkCenter> WorkCenters => Set<WorkCenter>();
    public DbSet<BillOfMaterials> Boms => Set<BillOfMaterials>();
    public DbSet<BomItem> BomItems => Set<BomItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionOutboxMessage> OutboxMessages => Set<ProductionOutboxMessage>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<ConsumptionRecord> ConsumptionRecords => Set<ConsumptionRecord>();
    public DbSet<ScrapRecord> ScrapRecords => Set<ScrapRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkCenter>(entity =>
        {
            entity.ToTable("work_centers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<BillOfMaterials>(entity =>
        {
            entity.ToTable("boms");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Version).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => new { x.ProductCode, x.Version }).IsUnique();

            entity.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.BomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<BomItem>(entity =>
        {
            entity.ToTable("bom_items");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.ToTable("production_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(32).IsRequired();

            entity.Property(x => x.ProductCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.PlannedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => new { x.WorkCenterId, x.Status });
        });

        modelBuilder.Entity<ProductionOutboxMessage>(entity =>
        {
            entity.ToTable("production_outbox");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.DispatchedAt);
            entity.Property(x => x.DeadLetteredAt);
            entity.Property(x => x.AttemptCount).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);

            // Covering index for the dispatcher poll query: pending rows only.
            entity.HasIndex(x => new { x.DispatchedAt, x.DeadLetteredAt });
        });

        modelBuilder.Entity<QualityInspection>(entity =>
        {
            entity.ToTable("quality_inspections");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductionOrderId).IsRequired();
            entity.Property(x => x.Result).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.InspectedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.InspectedAt).IsRequired();
            entity.HasIndex(x => new { x.ProductionOrderId, x.InspectedAt });
        });

        modelBuilder.Entity<ConsumptionRecord>(entity =>
        {
            entity.ToTable("consumption_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductionOrderId).IsRequired();

            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ConsumedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.InventoryBalanceId);
            entity.Property(x => x.RecordedAt).IsRequired();
            entity.HasIndex(x => x.ProductionOrderId);
        });

        modelBuilder.Entity<ScrapRecord>(entity =>
        {
            entity.ToTable("scrap_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProductionOrderId).IsRequired();

            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ScrapQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.RecordedAt).IsRequired();
            entity.HasIndex(x => x.ProductionOrderId);
        });
    }
}
