using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<InventoryBalance> Balances => Set<InventoryBalance>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<InventoryLedgerEntry> LedgerEntries => Set<InventoryLedgerEntry>();
    public DbSet<InventoryIntegrationMessage> ProcessedIntegrationMessages => Set<InventoryIntegrationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockLocation>(entity =>
        {
            entity.ToTable("stock_locations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("inventory_balances");
            entity.HasKey(x => x.Id);
            
            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,4)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.SourceReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.LotNumber).HasMaxLength(64);
            entity.Property(x => x.ExpirationDate);
            entity.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.SourceMetadata).HasColumnType("jsonb");
            entity.HasIndex(x => x.SourceReference).IsUnique();
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("materials");
            entity.HasKey(x => x.Id);
            
            entity.Property(x => x.MaterialCode)
                .HasConversion(v => v.Value, v => MaterialCode.From(v))
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.Ncm).HasMaxLength(16);
            entity.Property(x => x.Gtin).HasMaxLength(32);
            entity.Property(x => x.OfficialName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.HasIndex(x => x.MaterialCode).IsUnique();
        });

        modelBuilder.Entity<InventoryLedgerEntry>(entity =>
        {
            entity.ToTable("inventory_ledger_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Operation).HasMaxLength(64).IsRequired();
            entity.Property(x => x.QuantityDelta).HasColumnType("numeric(18,4)");
            entity.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => new { x.BalanceId, x.CreatedAt });
        });

        modelBuilder.Entity<InventoryIntegrationMessage>(entity =>
        {
            entity.ToTable("inventory_processed_integration_messages");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        });
    }
}
