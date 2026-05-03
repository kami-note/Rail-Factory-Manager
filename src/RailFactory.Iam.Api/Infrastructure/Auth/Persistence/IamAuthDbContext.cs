using Microsoft.EntityFrameworkCore;

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamAuthDbContext(DbContextOptions<IamAuthDbContext> options) : DbContext(options)
{
    public DbSet<IamLocalUserRecord> LocalUsers => Set<IamLocalUserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IamLocalUserRecord>();
        entity.ToTable("iam_local_users");
        entity.HasKey(x => new { x.ExternalProvider, x.ExternalSubject });

        entity.Property(x => x.ExternalProvider)
            .HasColumnName("external_provider")
            .HasColumnType("text");

        entity.Property(x => x.ExternalSubject)
            .HasColumnName("external_subject")
            .HasColumnType("text");

        entity.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("text");

        entity.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("text");

        entity.Property(x => x.FirstLoginAt)
            .HasColumnName("first_login_at")
            .HasColumnType("timestamp with time zone");

        entity.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        entity.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(x => x.Email)
            .HasDatabaseName("ix_iam_local_users_email");
    }
}
