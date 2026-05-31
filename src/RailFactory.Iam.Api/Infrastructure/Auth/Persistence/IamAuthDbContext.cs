using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Iam.Api.Domain;

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamAuthDbContext(
    DbContextOptions<IamAuthDbContext> options,
    ITenantContextAccessor? tenantContextAccessor = null) : DbContext(options)
{
    private readonly string? _tenantCode = tenantContextAccessor?.Current?.TenantCode;

    public DbSet<IamLocalUserRecord> LocalUsers => Set<IamLocalUserRecord>();
    public DbSet<IamTenantRoleRecord> Roles => Set<IamTenantRoleRecord>();
    public DbSet<IamTenantUserRoleRecord> UserRoles => Set<IamTenantUserRoleRecord>();
    public DbSet<IamAuditEntry> AuditEntries => Set<IamAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureLocalUsers(modelBuilder);
        ConfigureRoles(modelBuilder);
        ConfigureUserRoles(modelBuilder);
        ConfigureAuditEntries(modelBuilder);
    }

    private static void ConfigureLocalUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IamLocalUserRecord>();
        entity.ToTable("iam_local_users");
        entity.HasKey(x => new { x.ExternalProvider, x.ExternalSubject });

        entity.Property(x => x.ExternalProvider).HasColumnName("external_provider");
        entity.Property(x => x.ExternalSubject).HasColumnName("external_subject");
        entity.Property(x => x.Email).HasColumnName("email");
        entity.Property(x => x.DisplayName).HasColumnName("display_name");
        entity.Property(x => x.FirstLoginAt).HasColumnName("first_login_at");
        entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.Email).HasDatabaseName("ix_iam_local_users_email");
    }

    private void ConfigureRoles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IamTenantRoleRecord>();
        entity.ToTable("iam_tenant_roles");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.TenantCode).HasColumnName("tenant_code");
        entity.Property(x => x.Name).HasColumnName("name");
        entity.Property(x => x.Description).HasColumnName("description");
        
        entity.Property(x => x.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("jsonb");

        entity.Property(x => x.ChildRoleIds)
            .HasColumnName("child_role_ids")
            .HasColumnType("jsonb");

        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        entity.HasIndex(x => x.TenantCode).HasDatabaseName("ix_iam_tenant_roles_tenant_code");

        // Multi-tenancy filter
        entity.HasQueryFilter(x => x.TenantCode == _tenantCode);
    }

    private void ConfigureUserRoles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IamTenantUserRoleRecord>();
        entity.ToTable("iam_tenant_user_roles");
        entity.HasKey(x => new { x.TenantCode, x.ExternalProvider, x.ExternalSubject, x.RoleId });

        entity.Property(x => x.TenantCode).HasColumnName("tenant_code");
        entity.Property(x => x.ExternalProvider).HasColumnName("external_provider");
        entity.Property(x => x.ExternalSubject).HasColumnName("external_subject");
        entity.Property(x => x.RoleId).HasColumnName("role_id");
        entity.Property(x => x.AssignedAt).HasColumnName("assigned_at");

        entity.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Link to User (External Provider/Subject)
        entity.HasOne<IamLocalUserRecord>()
            .WithMany()
            .HasForeignKey(x => new { x.ExternalProvider, x.ExternalSubject })
            .OnDelete(DeleteBehavior.Cascade);

        // Multi-tenancy filter
        entity.HasQueryFilter(x => x.TenantCode == _tenantCode);
    }

    private static void ConfigureAuditEntries(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<IamAuditEntry>();
        entity.ToTable("iam_audit_entries");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id");
        entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        entity.Property(x => x.ActorEmail).HasColumnName("actor_email").HasMaxLength(200).IsRequired();
        entity.Property(x => x.AffectedEmail).HasColumnName("affected_email").HasMaxLength(200);
        entity.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        entity.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        entity.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_iam_audit_entries_occurred_at").IsDescending();
        entity.HasIndex(x => x.ActorEmail).HasDatabaseName("ix_iam_audit_entries_actor_email");
    }
}
