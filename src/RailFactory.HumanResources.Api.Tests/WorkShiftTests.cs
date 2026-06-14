using FluentAssertions;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="WorkShift"/> entity.
/// </summary>
public class WorkShiftTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(17, 0);
        var notes = "Day shift";

        // Act
        var shift = WorkShift.Create(personId, date, start, end, notes);

        // Assert
        shift.Id.Should().NotBeEmpty();
        shift.PersonId.Should().Be(personId);
        shift.ShiftDate.Should().Be(date);
        shift.StartTime.Should().Be(start);
        shift.EndTime.Should().Be(end);
        shift.Notes.Should().Be(notes);
    }

    [Fact]
    public void Create_WithEndTimeBeforeOrEqualStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var start = new TimeOnly(14, 0);
        var end = new TimeOnly(13, 59);

        // Act
        Action act = () => WorkShift.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), start, end);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*EndTime must be after StartTime*");
    }
}
