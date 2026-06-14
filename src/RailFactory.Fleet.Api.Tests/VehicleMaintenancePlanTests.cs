using FluentAssertions;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="VehicleMaintenancePlan"/> entity.
/// </summary>
public class VehicleMaintenancePlanTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var type = MaintenanceType.Preventive;
        var description = "Change engine oil";
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var notes = "Use 5W-30 oil";

        // Act
        var plan = VehicleMaintenancePlan.Create(vehicleId, type, description, date, notes);

        // Assert
        plan.Id.Should().NotBeEmpty();
        plan.VehicleId.Should().Be(vehicleId);
        plan.Type.Should().Be(type);
        plan.Description.Should().Be(description);
        plan.ScheduledDate.Should().Be(date);
        plan.Notes.Should().Be(notes);
        plan.Status.Should().Be(MaintenanceStatus.Scheduled);
        plan.CompletedDate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDescription_ShouldThrowArgumentException(string invalidDescription)
    {
        // Act
        Action act = () => VehicleMaintenancePlan.Create(Guid.NewGuid(), MaintenanceType.Corrective, invalidDescription, DateOnly.FromDateTime(DateTime.Today), null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Description is required*");
    }

    [Fact]
    public void Complete_WhenScheduled_ShouldChangeStatusToDoneAndSetCompletedDate()
    {
        // Arrange
        var plan = VehicleMaintenancePlan.Create(Guid.NewGuid(), MaintenanceType.Preventive, "Check brakes", DateOnly.FromDateTime(DateTime.Today), null);
        var completedDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        plan.Complete(completedDate);

        // Assert
        plan.Status.Should().Be(MaintenanceStatus.Done);
        plan.CompletedDate.Should().Be(completedDate);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var plan = VehicleMaintenancePlan.Create(Guid.NewGuid(), MaintenanceType.Preventive, "Check brakes", DateOnly.FromDateTime(DateTime.Today), null);
        plan.Complete(DateOnly.FromDateTime(DateTime.Today));

        // Act
        Action act = () => plan.Complete(DateOnly.FromDateTime(DateTime.Today));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only scheduled maintenance can be completed*");
    }

    [Fact]
    public void Cancel_WhenScheduled_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var plan = VehicleMaintenancePlan.Create(Guid.NewGuid(), MaintenanceType.Preventive, "Check brakes", DateOnly.FromDateTime(DateTime.Today), null);

        // Act
        plan.Cancel();

        // Assert
        plan.Status.Should().Be(MaintenanceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var plan = VehicleMaintenancePlan.Create(Guid.NewGuid(), MaintenanceType.Preventive, "Check brakes", DateOnly.FromDateTime(DateTime.Today), null);
        plan.Cancel();

        // Act
        Action act = () => plan.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only scheduled maintenance can be cancelled*");
    }
}
