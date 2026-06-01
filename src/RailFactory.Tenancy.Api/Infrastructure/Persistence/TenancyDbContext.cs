using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options)
{
    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();
    public DbSet<TenantIntegrationRecord> TenantIntegrations => Set<TenantIntegrationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureTenantIntegrations(modelBuilder);

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

    private static void ConfigureTenantIntegrations(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<TenantIntegrationRecord>();
        e.ToTable("tenant_integrations");
        e.HasKey(x => x.Id);

        e.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        e.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("text");
        e.Property(x => x.Category).HasColumnName("category").HasColumnType("varchar(50)");
        e.Property(x => x.ProviderType).HasColumnName("provider_type").HasColumnType("varchar(50)");
        e.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasColumnType("boolean");
        e.Property(x => x.EncryptedCredentials).HasColumnName("encrypted_credentials").HasColumnType("bytea");
        e.Property(x => x.CredentialsDek).HasColumnName("credentials_dek").HasColumnType("bytea");
        e.Property(x => x.CredentialsIv).HasColumnName("credentials_iv").HasColumnType("bytea");
        e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        e.HasIndex(x => new { x.TenantId, x.Category })
            .IsUnique()
            .HasDatabaseName("uix_tenant_integrations_tenant_category");
    }
}
