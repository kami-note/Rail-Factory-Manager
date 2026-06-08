using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Shipments;

public sealed record CreateShipmentOrderInput(
    Guid? ProductionOrderRef,
    string? Notes,
    decimal? DeliveryLatitudeDeg = null,
    decimal? DeliveryLongitudeDeg = null,
    string? DeliveryCity = null,
    string? RecipientCnpj = null,
    string? RecipientName = null,
    string? RecipientEmail = null,
    string? RecipientStreet = null,
    string? RecipientNumber = null,
    string? RecipientDistrict = null,
    string? RecipientCity = null,
    string? RecipientState = null,
    string? RecipientZipCode = null,
    string? NatureOfOperation = null,
    string? RecipientIe = null,
    int ModalidadeFrete = 0);

public sealed class CreateShipmentOrder(IShipmentOrderRepository orders)
{
    public async Task<ShipmentOrder> ExecuteAsync(CreateShipmentOrderInput input, CancellationToken ct)
    {
        var order = ShipmentOrder.Create(
            input.ProductionOrderRef, input.Notes,
            input.DeliveryLatitudeDeg, input.DeliveryLongitudeDeg, input.DeliveryCity,
            input.RecipientCnpj, input.RecipientName, input.RecipientEmail,
            input.RecipientStreet, input.RecipientNumber, input.RecipientDistrict,
            input.RecipientCity, input.RecipientState, input.RecipientZipCode,
            input.NatureOfOperation,
            input.RecipientIe, input.ModalidadeFrete);
        await orders.SaveAsync(order, ct);
        return order;
    }
}
