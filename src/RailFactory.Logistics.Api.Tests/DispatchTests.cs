using FluentAssertions;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="Dispatch"/> entity.
/// </summary>
public class DispatchTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var freight = 250.5m;
        var plate = "ABC-1234";
        var rntrc = "123456";
        var cpf = "123.456.789-00";
        var driverName = "Carlos Silva";

        // Act
        var dispatch = Dispatch.Create(orderId, carrierId, vehicleId, driverId, freight, plate, rntrc, cpf, driverName);

        // Assert
        dispatch.Id.Should().NotBeEmpty();
        dispatch.ShipmentOrderId.Should().Be(orderId);
        dispatch.CarrierId.Should().Be(carrierId);
        dispatch.VehicleId.Should().Be(vehicleId);
        dispatch.DriverPersonId.Should().Be(driverId);
        dispatch.TrackingCode.Should().StartWith("RF-");
        dispatch.FreightValueBrl.Should().Be(freight);
        dispatch.Status.Should().Be(DispatchStatus.Pending);
        dispatch.VehiclePlate.Should().Be(plate);
        dispatch.VehicleRntrc.Should().Be(rntrc);
        dispatch.DriverCpf.Should().Be(cpf);
        dispatch.DriverName.Should().Be(driverName);
    }

    [Fact]
    public void Conference_WhenPending_ShouldSetConferencedAt()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);

        // Act
        dispatch.Conference();

        // Assert
        dispatch.ConferencedAt.Should().NotBeNull();
    }

    [Fact]
    public void Conference_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);
        dispatch.Ship();

        // Act
        Action act = () => dispatch.Conference();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only Pending dispatches can be conferenced*");
    }

    [Fact]
    public void Ship_WhenPending_ShouldTransitionToInTransitAndSetDispatchedAt()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);

        // Act
        dispatch.Ship();

        // Assert
        dispatch.Status.Should().Be(DispatchStatus.InTransit);
        dispatch.DispatchedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deliver_WhenInTransit_ShouldTransitionToDeliveredAndSetDeliveredAt()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);
        dispatch.Ship();

        // Act
        dispatch.Deliver();

        // Assert
        dispatch.Status.Should().Be(DispatchStatus.Delivered);
        dispatch.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void Return_WhenInTransit_ShouldTransitionToReturned()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);
        dispatch.Ship();

        // Act
        dispatch.Return();

        // Assert
        dispatch.Status.Should().Be(DispatchStatus.Returned);
    }

    [Fact]
    public void UpdateFiscalStatus_ShouldUpdateProperties()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);

        // Act
        dispatch.UpdateFiscalStatus("ext-id", "Authorized", "key123", null, "http://pdf", "http://xml");

        // Assert
        dispatch.FiscalExternalId.Should().Be("ext-id");
        dispatch.FiscalStatus.Should().Be("Authorized");
        dispatch.FiscalAccessKey.Should().Be("key123");
        dispatch.FiscalErrorMessage.Should().BeNull();
        dispatch.FiscalPdfUrl.Should().Be("http://pdf");
        dispatch.FiscalXmlUrl.Should().Be("http://xml");
    }

    [Fact]
    public void RequestFiscalRetry_WhenInTransit_ShouldClearFiscalFields()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);
        dispatch.Ship();
        dispatch.UpdateFiscalStatus("ext-id", "Error", "key", "invalid NCM", "http://pdf", "http://xml");

        // Act
        dispatch.RequestFiscalRetry();

        // Assert
        dispatch.FiscalStatus.Should().BeNull();
        dispatch.FiscalErrorMessage.Should().BeNull();
        dispatch.FiscalAccessKey.Should().BeNull();
        dispatch.FiscalExternalId.Should().BeNull();
    }

    [Fact]
    public void RequestFiscalRetry_WhenPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dispatch = Dispatch.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, 100m);

        // Act
        Action act = () => dispatch.RequestFiscalRetry();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only InTransit dispatches can have their fiscal emission retried*");
    }
}
