using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class LogisticsDbContext(DbContextOptions<LogisticsDbContext> options) : DbContext(options)
{
    public DbSet<Carrier> Carriers => Set<Carrier>();
    public DbSet<ShipmentOrder> ShipmentOrders => Set<ShipmentOrder>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<Dispatch> Dispatches => Set<Dispatch>();
    public DbSet<LogisticsOutboxMessage> OutboxMessages => Set<LogisticsOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Carrier>(entity =>
        {
            entity.ToTable("carriers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ContactEmail).HasMaxLength(200);
            entity.Property(x => x.WebhookUrl).HasMaxLength(2000);
            entity.Property(x => x.RatePerKg).HasColumnType("numeric(10,4)").IsRequired();
            entity.Property(x => x.RatePerCbm).HasColumnType("numeric(10,4)").IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.DocumentNumber).IsUnique();
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ShipmentOrder>(entity =>
        {
            entity.ToTable("shipment_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ProductionOrderRef);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => x.Status);

            entity.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.ShipmentOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShipmentItem>(entity =>
        {
            entity.ToTable("shipment_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ShipmentOrderId).IsRequired();
            entity.Property(x => x.MaterialCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("numeric(10,3)").IsRequired();
            entity.Property(x => x.UnitOfMeasure).HasMaxLength(20).IsRequired();
            entity.Property(x => x.WeightKg).HasColumnType("numeric(10,3)").IsRequired();
            entity.Property(x => x.VolumeCbm).HasColumnType("numeric(10,4)").IsRequired();
            entity.HasIndex(x => x.ShipmentOrderId);
        });

        modelBuilder.Entity<LogisticsOutboxMessage>(entity =>
        {
            entity.ToTable("logistics_outbox");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.DispatchedAt);
            entity.Property(x => x.DeadLetteredAt);
            entity.Property(x => x.AttemptCount).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => new { x.DispatchedAt, x.DeadLetteredAt });
        });

        modelBuilder.Entity<Dispatch>(entity =>
        {
            entity.ToTable("dispatches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ShipmentOrderId).IsRequired();
            entity.Property(x => x.CarrierId).IsRequired();
            entity.Property(x => x.VehicleId);
            entity.Property(x => x.DriverPersonId);
            entity.Property(x => x.TrackingCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FreightValueBrl).HasColumnType("numeric(12,2)").IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.ConferencedAt);
            entity.Property(x => x.DispatchedAt);
            entity.Property(x => x.DeliveredAt);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.TrackingCode).IsUnique();
            entity.HasIndex(x => x.ShipmentOrderId);
            entity.HasIndex(x => x.Status);
        });
    }
}
