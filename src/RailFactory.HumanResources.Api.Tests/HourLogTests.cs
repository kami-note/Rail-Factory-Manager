using FluentAssertions;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="HourLog"/> entity.
/// </summary>
public class HourLogTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var hours = 8.5m;
        var description = "Assembled TR-100 batch";

        // Act
        var log = HourLog.Create(personId, date, hours, description);

        // Assert
        log.Id.Should().NotBeEmpty();
        log.PersonId.Should().Be(personId);
        log.Date.Should().Be(date);
        log.HoursWorked.Should().Be(hours);
        log.Description.Should().Be(description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24.1)]
    [InlineData(30)]
    public void Create_WithInvalidHours_ShouldThrowArgumentOutOfRangeException(decimal invalidHours)
    {
        // Act
        Action act = () => HourLog.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), invalidHours, null);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Hours worked must be between 0 and 24*");
    }
}
