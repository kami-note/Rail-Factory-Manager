using FluentAssertions;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Tests;

/// <summary>
/// Unit tests for the <see cref="ShipmentOrder"/> aggregate and <see cref="ShipmentItem"/> entity.
/// </summary>
public class ShipmentOrderTests
{
    [Fact]
    public void Create_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var prodRef = Guid.NewGuid();
        var notes = "Special care requested";
        var lat = -23.55052m;
        var lon = -46.633308m;
        var city = "São Paulo";
        var cnpj = "12345678901234";
        var name = "Client ACME";
        var email = "acme@example.com";
        var ie = "111222333";

        // Act
        var order = ShipmentOrder.Create(
            prodRef, notes, lat, lon, city, cnpj, name, email,
            "Rua A", "123", "Centro", city, "SP", "01001-000",
            "Venda", ie, 1
        );

        // Assert
        order.Id.Should().NotBeEmpty();
        order.OrderNumber.Should().StartWith("EXP-");
        order.ProductionOrderRef.Should().Be(prodRef);
        order.Notes.Should().Be(notes);
        order.Status.Should().Be(ShipmentOrderStatus.Draft);
        order.DeliveryLatitudeDeg.Should().Be(lat);
        order.DeliveryLongitudeDeg.Should().Be(lon);
        order.DeliveryCity.Should().Be(city);
        order.RecipientCnpj.Should().Be(cnpj);
        order.RecipientName.Should().Be(name);
        order.RecipientEmail.Should().Be(email);
        order.RecipientIe.Should().Be(ie);
        order.ModalidadeFrete.Should().Be(1);
        order.NatureOfOperation.Should().Be("Venda");
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_WhenDraft_ShouldAddItemAndSetProperties()
    {
        // Arrange
        var order = ShipmentOrder.Create(null, null);

        // Act
        var item = order.AddItem(
            "  mat-code-1  ", 10.5m, "  un  ", 2.5m, 0.12m,
            "12345678", "5102", 50.0m, 500.0m, 18.0m, 0, "00", "01", "01", 5.0m, "99"
        );

        // Assert
        order.Items.Should().ContainSingle();
        item.ShipmentOrderId.Should().Be(order.Id);
        item.MaterialCode.Should().Be("MAT-CODE-1"); // trimmed and uppercased
        item.Quantity.Should().Be(10.5m);
        item.UnitOfMeasure.Should().Be("un");
        item.WeightKg.Should().Be(2.5m);
        item.VolumeCbm.Should().Be(0.12m);
        item.NcmCode.Should().Be("12345678");
        item.CfopCode.Should().Be("5102");
        item.UnitValue.Should().Be(50.0m);
        item.TaxBaseIcms.Should().Be(500.0m);
        item.IcmsRate.Should().Be(18.0m);
        item.IcmsOrigin.Should().Be(0);
        item.IcmsCst.Should().Be("00");
        item.PisCst.Should().Be("01");
        item.CofinsCst.Should().Be("01");
        item.IpiRate.Should().Be(5.0m);
        item.IpiCst.Should().Be("99");
    }

    [Fact]
    public void AddItem_WhenNotDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ShipmentOrder.Create(null, null);
        order.StartPicking();

        // Act
        Action act = () => order.AddItem("MAT", 10m, "UN", 1m, 1m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Items can only be added to Draft orders*");
    }

    [Fact]
    public void Transitions_ShouldFollowWorkflowCorrectly()
    {
        // Arrange
        var order = ShipmentOrder.Create(null, null);

        // Act & Assert (Draft -> Picking)
        order.StartPicking();
        order.Status.Should().Be(ShipmentOrderStatus.Picking);

        // Act & Assert (Picking -> Packing)
        order.StartPacking();
        order.Status.Should().Be(ShipmentOrderStatus.Packing);

        // Act & Assert (Packing -> ReadyToShip)
        order.MarkReadyToShip();
        order.Status.Should().Be(ShipmentOrderStatus.ReadyToShip);

        // Act & Assert (ReadyToShip -> Shipped)
        order.MarkShipped();
        order.Status.Should().Be(ShipmentOrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_WhenDraftOrPickingOrPackingOrReadyToShip_ShouldSetStatusToCancelled()
    {
        // Arrange & Act (Draft -> Cancelled)
        var order1 = ShipmentOrder.Create(null, null);
        order1.Cancel();
        order1.Status.Should().Be(ShipmentOrderStatus.Cancelled);

        // Arrange & Act (Picking -> Cancelled)
        var order2 = ShipmentOrder.Create(null, null);
        order2.StartPicking();
        order2.Cancel();
        order2.Status.Should().Be(ShipmentOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenShipped_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = ShipmentOrder.Create(null, null);
        order.StartPicking();
        order.StartPacking();
        order.MarkReadyToShip();
        order.MarkShipped();

        // Act
        Action act = () => order.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*orders cannot be cancelled*");
    }
}
