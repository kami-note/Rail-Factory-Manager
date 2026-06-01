namespace RailFactory.HumanResources.Api.Domain;

/// <summary>
/// Represents a technical skill in a person's competency matrix (RF-32).
/// </summary>
public sealed class PersonSkill
{
    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public string SkillName { get; private set; }

    /// <summary>Proficiency level from 1 (basic) to 5 (expert).</summary>
    public int ProficiencyLevel { get; private set; }

    public DateOnly? CertifiedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PersonSkill()
    {
        SkillName = string.Empty;
    }

    private PersonSkill(Guid id, Guid personId, string skillName, int proficiencyLevel, DateOnly? certifiedAt, string? notes)
    {
        Id = id;
        PersonId = personId;
        SkillName = skillName;
        ProficiencyLevel = proficiencyLevel;
        CertifiedAt = certifiedAt;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static PersonSkill Create(Guid personId, string skillName, int proficiencyLevel, DateOnly? certifiedAt = null, string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);
        if (proficiencyLevel is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(proficiencyLevel), "Proficiency level must be between 1 and 5.");

        return new PersonSkill(Guid.NewGuid(), personId, skillName.Trim(), proficiencyLevel, certifiedAt, notes?.Trim());
    }
}
