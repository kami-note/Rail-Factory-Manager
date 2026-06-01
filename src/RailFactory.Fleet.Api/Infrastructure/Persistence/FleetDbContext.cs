using Microsoft.EntityFrameworkCore;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<DriverAssignment> DriverAssignments => Set<DriverAssignment>();
    public DbSet<VehicleMaintenancePlan> MaintenancePlans => Set<VehicleMaintenancePlan>();
    public DbSet<FuelingRecord> FuelingRecords => Set<FuelingRecord>();
    public DbSet<VehicleTelemetryEvent> TelemetryEvents => Set<VehicleTelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Plate).HasMaxLength(12).IsRequired();
            entity.Property(x => x.Chassis).HasMaxLength(17).IsRequired();
            entity.Property(x => x.Renavam).HasMaxLength(15).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.MaxWeightKg).HasColumnType("numeric(10,2)").IsRequired();
            entity.Property(x => x.MaxVolumeCbm).HasColumnType("numeric(10,3)").IsRequired();
            entity.Property(x => x.LicenseExpiry).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();

            entity.HasIndex(x => x.Plate).IsUnique();
            entity.HasIndex(x => x.Status);

            entity.HasMany(x => x.Assignments)
                .WithOne()
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Assignments)
                .HasField("_assignments")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DriverAssignment>(entity =>
        {
            entity.ToTable("driver_assignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VehicleId).IsRequired();
            entity.Property(x => x.DriverPersonId).IsRequired();
            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.EndDate);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.AssignedAt).IsRequired();
            entity.HasIndex(x => new { x.VehicleId, x.StartDate });
            entity.HasIndex(x => x.DriverPersonId);
        });

        modelBuilder.Entity<VehicleMaintenancePlan>(entity =>
        {
            entity.ToTable("maintenance_plans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VehicleId).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ScheduledDate).IsRequired();
            entity.Property(x => x.CompletedDate);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<FuelingRecord>(entity =>
        {
            entity.ToTable("fueling_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VehicleId).IsRequired();
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.LitersSupplied).HasColumnType("numeric(10,3)").IsRequired();
            entity.Property(x => x.PricePerLiter).HasColumnType("numeric(10,4)").IsRequired();
            entity.Property(x => x.Odometer);
            entity.Property(x => x.Supplier).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.RecordedAt).IsRequired();
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<VehicleTelemetryEvent>(entity =>
        {
            entity.ToTable("vehicle_telemetry_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VehicleId).IsRequired();
            entity.Property(x => x.DriverPersonId);
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.LatitudeDeg).HasColumnType("numeric(10,6)");
            entity.Property(x => x.LongitudeDeg).HasColumnType("numeric(10,6)");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.VehicleId, x.OccurredAt });
            entity.HasIndex(x => x.EventType);
        });
    }
}
