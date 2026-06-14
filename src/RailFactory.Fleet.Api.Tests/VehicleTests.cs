using FluentAssertions;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Vehicle"/> aggregate root.
/// </summary>
public class VehicleTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var plate = "abc-1234";
        var chassis = "9382103820138219a";
        var renavam = "12345678901";
        var rntrc = "12345678";
        var type = VehicleType.Truck;
        var maxWeight = 12000.5m;
        var maxVolume = 45.2m;
        var licenseExpiry = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        // Act
        var vehicle = Vehicle.Create(plate, chassis, renavam, rntrc, type, maxWeight, maxVolume, licenseExpiry);

        // Assert
        vehicle.Id.Should().NotBeEmpty();
        vehicle.Plate.Should().Be("ABC-1234"); // Should trim and uppercase
        vehicle.Chassis.Should().Be("9382103820138219A"); // Should trim and uppercase
        vehicle.Renavam.Should().Be("12345678901");
        vehicle.Rntrc.Should().Be("12345678");
        vehicle.Type.Should().Be(type);
        vehicle.Status.Should().Be(VehicleStatus.Active);
        vehicle.MaxWeightKg.Should().Be(maxWeight);
        vehicle.MaxVolumeCbm.Should().Be(maxVolume);
        vehicle.LicenseExpiry.Should().Be(licenseExpiry);
    }

    [Theory]
    [InlineData("", "chassis", "renavam")]
    [InlineData("plate", "", "renavam")]
    [InlineData("plate", "chassis", "")]
    [InlineData("   ", "chassis", "renavam")]
    public void Create_WithEmptyOrWhitespaceArguments_ShouldThrowArgumentException(string plate, string chassis, string renavam)
    {
        // Arrange
        var licenseExpiry = DateOnly.FromDateTime(DateTime.Today);

        // Act
        Action act = () => Vehicle.Create(plate, chassis, renavam, "rntrc", VehicleType.Car, 1000m, 10m, licenseExpiry);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithNegativeWeight_ShouldThrowArgumentOutOfRangeException(decimal invalidWeight)
    {
        // Arrange
        var licenseExpiry = DateOnly.FromDateTime(DateTime.Today);

        // Act
        Action act = () => Vehicle.Create("plate", "chassis", "renavam", "rntrc", VehicleType.Car, invalidWeight, 10m, licenseExpiry);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithNegativeVolume_ShouldThrowArgumentOutOfRangeException(decimal invalidVolume)
    {
        // Arrange
        var licenseExpiry = DateOnly.FromDateTime(DateTime.Today);

        // Act
        Action act = () => Vehicle.Create("plate", "chassis", "renavam", "rntrc", VehicleType.Car, 1000m, invalidVolume, licenseExpiry);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetRntrc_ShouldUpdateRntrcAndTrim()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", "old", VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));

        // Act
        vehicle.SetRntrc("  new-rntrc  ");

        // Assert
        vehicle.Rntrc.Should().Be("new-rntrc");
    }

    [Fact]
    public void Deactivate_ShouldChangeStatusToInactive()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));

        // Act
        vehicle.Deactivate();

        // Assert
        vehicle.Status.Should().Be(VehicleStatus.Inactive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));
        vehicle.Deactivate();

        // Act
        Action act = () => vehicle.Deactivate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive_WhenInactive()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));
        vehicle.Deactivate();

        // Act
        vehicle.Activate();

        // Assert
        vehicle.Status.Should().Be(VehicleStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));

        // Act
        Action act = () => vehicle.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void AssignDriver_WhenActive_ShouldAddAssignment()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));
        var driverId = Guid.NewGuid();
        var startDate = DateOnly.FromDateTime(DateTime.Today);

        // Act
        var assignment = vehicle.AssignDriver(driverId, startDate, null, "First driver");

        // Assert
        vehicle.Assignments.Should().ContainSingle();
        assignment.VehicleId.Should().Be(vehicle.Id);
        assignment.DriverPersonId.Should().Be(driverId);
        assignment.StartDate.Should().Be(startDate);
        assignment.EndDate.Should().BeNull();
        assignment.Notes.Should().Be("First driver");
    }

    [Fact]
    public void AssignDriver_WhenInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var vehicle = Vehicle.Create("abc-1234", "chassis", "renavam", null, VehicleType.Van, 1000m, 10m, DateOnly.FromDateTime(DateTime.Today));
        vehicle.Deactivate();

        // Act
        Action act = () => vehicle.AssignDriver(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), null, null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot assign a driver to an inactive vehicle*");
    }
}
