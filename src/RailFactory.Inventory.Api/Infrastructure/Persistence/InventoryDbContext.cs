using Microsoft.EntityFrameworkCore;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<InventoryBalance> Balances => Set<InventoryBalance>();
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
            entity.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,4)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.SourceReference).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.SourceReference).IsUnique();
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
