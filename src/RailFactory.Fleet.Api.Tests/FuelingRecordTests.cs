using FluentAssertions;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="FuelingRecord"/> entity.
/// </summary>
public class FuelingRecordTests
{
    [Fact]
    public void Create_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var liters = 50.5m;
        var price = 5.89m;
        var odometer = 120500;
        var supplier = "Posto BR";
        var notes = "Filled up with Diesel S10";

        // Act
        var record = FuelingRecord.Create(vehicleId, date, liters, price, odometer, supplier, notes);

        // Assert
        record.Id.Should().NotBeEmpty();
        record.VehicleId.Should().Be(vehicleId);
        record.Date.Should().Be(date);
        record.LitersSupplied.Should().Be(liters);
        record.PricePerLiter.Should().Be(price);
        record.Odometer.Should().Be(odometer);
        record.Supplier.Should().Be(supplier);
        record.Notes.Should().Be(notes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithInvalidLiters_ShouldThrowArgumentException(decimal invalidLiters)
    {
        // Act
        Action act = () => FuelingRecord.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), invalidLiters, 5.89m, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Liters supplied must be positive*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Create_WithInvalidPrice_ShouldThrowArgumentException(decimal invalidPrice)
    {
        // Act
        Action act = () => FuelingRecord.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 50m, invalidPrice, null, null, null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Price per liter must be positive*");
    }
}
