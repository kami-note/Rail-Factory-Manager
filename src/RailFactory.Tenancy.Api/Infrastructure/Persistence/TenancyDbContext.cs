using Microsoft.EntityFrameworkCore;

namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<TenantRecord>();
        entity.ToTable("tenants");
        entity.HasKey(x => x.Code);

        entity.Property(x => x.Code).HasColumnName("code").HasColumnType("text");
        entity.Property(x => x.DisplayName).HasColumnName("display_name").HasColumnType("text");
        entity.Property(x => x.Locale).HasColumnName("locale").HasColumnType("text");
        entity.Property(x => x.TimeZone).HasColumnName("time_zone").HasColumnType("text");
        entity.Property(x => x.Status).HasColumnName("status").HasColumnType("text");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        entity.HasIndex(x => x.Status).HasDatabaseName("ix_tenants_status");
    }
}
