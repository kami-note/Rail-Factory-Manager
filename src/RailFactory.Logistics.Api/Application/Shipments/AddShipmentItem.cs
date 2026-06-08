using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Shipments;

public sealed record AddShipmentItemInput(
    Guid OrderId, string MaterialCode, decimal Quantity,
    string UnitOfMeasure, decimal WeightKg, decimal VolumeCbm,
    string NcmCode = "", string CfopCode = "",
    decimal UnitValue = 0, decimal TaxBaseIcms = 0, decimal IcmsRate = 12,
    int IcmsOrigin = 0, string IcmsCst = "40", string PisCst = "07", string CofinsCst = "07",
    decimal IpiRate = 0, string IpiCst = "99");

public sealed class AddShipmentItem(IShipmentOrderRepository orders)
{
    public async Task<ShipmentItem> ExecuteAsync(AddShipmentItemInput input, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(input.OrderId, ct)
            ?? throw new KeyNotFoundException($"Shipment order {input.OrderId} not found.");

        var item = order.AddItem(input.MaterialCode, input.Quantity,
            input.UnitOfMeasure, input.WeightKg, input.VolumeCbm,
            input.NcmCode, input.CfopCode, input.UnitValue, input.TaxBaseIcms, input.IcmsRate,
            input.IcmsOrigin, input.IcmsCst, input.PisCst, input.CofinsCst, input.IpiRate, input.IpiCst);

        await orders.AddItemDirectAsync(order.Id, item, order.UpdatedAt, ct);
        return item;
    }
}
