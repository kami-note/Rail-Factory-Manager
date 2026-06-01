using Microsoft.EntityFrameworkCore;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class HrDbContext(DbContextOptions<HrDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();
    public DbSet<HourLog> HourLogs => Set<HourLog>();
    public DbSet<PersonSkill> PersonSkills => Set<PersonSkill>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("people");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.DocumentNumber).IsUnique();
            entity.HasIndex(x => new { x.Type, x.Status });
        });

        modelBuilder.Entity<HourLog>(entity =>
        {
            entity.ToTable("hour_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PersonId).IsRequired();
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.HoursWorked).HasColumnType("numeric(5,2)").IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.RecordedAt).IsRequired();
            entity.HasIndex(x => new { x.PersonId, x.Date });
        });

        modelBuilder.Entity<PersonSkill>(entity =>
        {
            entity.ToTable("person_skills");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PersonId).IsRequired();
            entity.Property(x => x.SkillName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ProficiencyLevel).IsRequired();
            entity.Property(x => x.CertifiedAt);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.PersonId, x.SkillName }).IsUnique();
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.ToTable("work_shifts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PersonId).IsRequired();
            entity.Property(x => x.ShiftDate).IsRequired();
            entity.Property(x => x.StartTime).IsRequired();
            entity.Property(x => x.EndTime).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => new { x.PersonId, x.ShiftDate });
        });
    }
}
