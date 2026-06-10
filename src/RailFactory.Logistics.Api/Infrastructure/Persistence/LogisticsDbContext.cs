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
    public DbSet<InboundWebhookEvent> InboundWebhookEvents => Set<InboundWebhookEvent>();
    public DbSet<TenantFiscalProfile> FiscalProfiles => Set<TenantFiscalProfile>();

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
            entity.Property(x => x.NatureOfOperation).HasMaxLength(60).IsRequired();
            entity.Property(x => x.RecipientCnpj).HasMaxLength(20);
            entity.Property(x => x.RecipientName).HasMaxLength(200);
            entity.Property(x => x.RecipientEmail).HasMaxLength(200);
            entity.Property(x => x.RecipientStreet).HasMaxLength(200);
            entity.Property(x => x.RecipientNumber).HasMaxLength(10);
            entity.Property(x => x.RecipientDistrict).HasMaxLength(100);
            entity.Property(x => x.RecipientCity).HasMaxLength(120);
            entity.Property(x => x.RecipientState).HasMaxLength(2);
            entity.Property(x => x.RecipientZipCode).HasMaxLength(10);
            entity.Property(x => x.RecipientIe).HasMaxLength(20);
            entity.Property(x => x.ModalidadeFrete).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.DeliveryLatitudeDeg).HasColumnType("numeric(10,6)");
            entity.Property(x => x.DeliveryLongitudeDeg).HasColumnType("numeric(10,6)");
            entity.Property(x => x.DeliveryCity).HasMaxLength(120);
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
            entity.Property(x => x.NcmCode).HasMaxLength(10);
            entity.Property(x => x.CfopCode).HasMaxLength(10);
            entity.Property(x => x.UnitValue).HasColumnType("numeric(14,4)");
            entity.Property(x => x.TaxBaseIcms).HasColumnType("numeric(14,4)");
            entity.Property(x => x.IcmsRate).HasColumnType("numeric(5,2)");
            entity.Property(x => x.IcmsOrigin).IsRequired();
            entity.Property(x => x.IcmsCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PisCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CofinsCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.IpiRate).HasColumnType("numeric(5,2)");
            entity.Property(x => x.IpiCst).HasMaxLength(3).IsRequired();
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

        modelBuilder.Entity<InboundWebhookEvent>(entity =>
        {
            entity.ToTable("logistics_inbound_webhook_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ExternalId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.RetryCount).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.Property(x => x.ReceivedAt).IsRequired();
            entity.Property(x => x.ProcessedAt);
            // Idempotency: one provider+externalId per tenant
            entity.HasIndex(x => new { x.Provider, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Status, x.ReceivedAt });
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
            entity.Property(x => x.FiscalExternalId).HasMaxLength(200);
            entity.Property(x => x.FiscalAccessKey).HasMaxLength(100);
            entity.Property(x => x.FiscalStatus).HasMaxLength(50);
            entity.Property(x => x.FiscalErrorMessage).HasMaxLength(500);
            entity.Property(x => x.FiscalPdfUrl).HasMaxLength(2000);
            entity.Property(x => x.FiscalXmlUrl).HasMaxLength(2000);
            entity.Property(x => x.MdfeExternalId).HasMaxLength(200);
            entity.Property(x => x.MdfeAccessKey).HasMaxLength(100);
            entity.Property(x => x.MdfeStatus).HasMaxLength(50);
            entity.Property(x => x.MdfeErrorMessage).HasMaxLength(500);
            entity.Property(x => x.MdfePdfUrl).HasMaxLength(2000);
            entity.Property(x => x.MdfeLinkedNfeKey).HasMaxLength(100);
            entity.Property(x => x.MdfeUfCarregamento).HasMaxLength(2);
            entity.Property(x => x.MdfeUfDescarregamento).HasMaxLength(2);
            entity.Property(x => x.ShippingExternalId).HasMaxLength(200);
            entity.Property(x => x.ShippingStatus).HasMaxLength(50);
            entity.Property(x => x.ShippingLabelUrl).HasMaxLength(2000);
            entity.Property(x => x.ShippingTrackingCode).HasMaxLength(100);
            entity.Property(x => x.ShippingErrorMessage).HasMaxLength(500);
            entity.Property(x => x.PaymentExternalId).HasMaxLength(200);
            entity.Property(x => x.PaymentStatus).HasMaxLength(50);
            entity.Property(x => x.PaymentBoletoUrl).HasMaxLength(2000);
            entity.Property(x => x.PaymentPixUrl).HasMaxLength(2000);
            entity.Property(x => x.PaymentErrorMessage).HasMaxLength(500);
            entity.Property(x => x.VehiclePlate).HasMaxLength(15);
            entity.Property(x => x.VehicleRntrc).HasMaxLength(20);
            entity.Property(x => x.DriverCpf).HasMaxLength(20);
            entity.Property(x => x.DriverName).HasMaxLength(200);
            entity.HasIndex(x => x.TrackingCode).IsUnique();
            entity.HasIndex(x => x.ShipmentOrderId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.FiscalExternalId);
            entity.HasIndex(x => x.ShippingExternalId);
            entity.HasIndex(x => x.PaymentExternalId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<TenantFiscalProfile>(entity =>
        {
            entity.ToTable("tenant_fiscal_profile");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CfopPadraoIntraestadual).HasMaxLength(5).IsRequired();
            entity.Property(x => x.CfopPadraoInterestadual).HasMaxLength(5).IsRequired();
            entity.Property(x => x.UfOrigem).HasMaxLength(2).IsRequired();
            entity.Property(x => x.IcmsRate).HasColumnType("numeric(5,2)").IsRequired();
            entity.Property(x => x.IcmsCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PisCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CofinsCst).HasMaxLength(3).IsRequired();
            entity.Property(x => x.IpiRate).HasColumnType("numeric(5,2)").IsRequired();
            entity.Property(x => x.IcmsOrigin).IsRequired();
            entity.Property(x => x.EmitterName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmitterCnpj).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EmitterIe).HasMaxLength(30).IsRequired();
            entity.Property(x => x.EmitterCity).HasMaxLength(120).IsRequired();
            entity.Property(x => x.EmitterState).HasMaxLength(2).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }
}
