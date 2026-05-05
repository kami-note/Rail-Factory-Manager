using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

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
        var connectionStringsConverter = new ValueConverter<Dictionary<string, string>, string>(
            toProvider => JsonSerializer.Serialize(toProvider, (JsonSerializerOptions?)null),
            fromProvider => JsonSerializer.Deserialize<Dictionary<string, string>>(fromProvider, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        var connectionStringsComparer = new ValueComparer<Dictionary<string, string>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
            value => value.ToDictionary(entry => entry.Key, entry => entry.Value));

        entity.Property(x => x.ConnectionStrings)
            .HasColumnName("connection_strings")
            .HasColumnType("jsonb")
            .HasConversion(connectionStringsConverter)
            .Metadata.SetValueComparer(connectionStringsComparer);
        entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        entity.HasIndex(x => x.Status).HasDatabaseName("ix_tenants_status");
    }
}
