using FluentAssertions;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="PersonSkill"/> entity.
/// </summary>
public class PersonSkillTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var skillName = "Welding";
        var level = 4;
        var date = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
        var notes = "Certified MIG welder";

        // Act
        var skill = PersonSkill.Create(personId, skillName, level, date, notes);

        // Assert
        skill.Id.Should().NotBeEmpty();
        skill.PersonId.Should().Be(personId);
        skill.SkillName.Should().Be(skillName);
        skill.ProficiencyLevel.Should().Be(level);
        skill.CertifiedAt.Should().Be(date);
        skill.Notes.Should().Be(notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidSkillName_ShouldThrowArgumentException(string invalidName)
    {
        // Act
        Action act = () => PersonSkill.Create(Guid.NewGuid(), invalidName, 3);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Create_WithInvalidProficiencyLevel_ShouldThrowArgumentOutOfRangeException(int invalidLevel)
    {
        // Act
        Action act = () => PersonSkill.Create(Guid.NewGuid(), "Welding", invalidLevel);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Proficiency level must be between 1 and 5*");
    }
}
