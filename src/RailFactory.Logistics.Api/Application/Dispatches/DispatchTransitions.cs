using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Dispatches;

public sealed class ConferenceDispatch(IDispatchRepository dispatches)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var dispatch = await dispatches.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Dispatch {id} not found.");
        dispatch.Conference();
        await dispatches.SaveAsync(dispatch, ct);
    }
}

public sealed class ShipDispatch(
    IDispatchRepository dispatches,
    IShipmentOrderRepository orders,
    ICarrierRepository carriers,
    ILogisticsOutboxRepository outbox)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var dispatch = await dispatches.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Dispatch {id} not found.");

        var order = await orders.GetByIdAsync(dispatch.ShipmentOrderId, ct)
            ?? throw new KeyNotFoundException($"ShipmentOrder {dispatch.ShipmentOrderId} not found.");

        var carrier = await carriers.GetByIdAsync(dispatch.CarrierId, ct)
            ?? throw new KeyNotFoundException($"Carrier {dispatch.CarrierId} not found.");

        dispatch.Ship();
        order.MarkShipped();

        if (order.Items.Count > 0)
        {
            var payload = JsonSerializer.Serialize(new
            {
                dispatchId = dispatch.Id,
                trackingCode = dispatch.TrackingCode,
                shipmentOrderId = order.Id,
                orderNumber = order.OrderNumber,
                items = order.Items.Select(i => new
                {
                    itemId = i.Id,
                    materialCode = i.MaterialCode,
                    quantity = i.Quantity,
                    unitOfMeasure = i.UnitOfMeasure
                })
            });

            await outbox.AddAsync(
                LogisticsOutboxMessage.Create(IntegrationConstants.LogisticsEvents.ShipmentDispatched, payload),
                ct);
        }

        if (carrier.WebhookUrl is not null)
        {
            var webhookPayload = JsonSerializer.Serialize(new
            {
                dispatchId = dispatch.Id,
                trackingCode = dispatch.TrackingCode,
                shipmentOrderId = order.Id,
                orderNumber = order.OrderNumber,
                newStatus = "InTransit",
                webhookUrl = carrier.WebhookUrl,
                carrierName = carrier.Name,
                occurredAt = DateTimeOffset.UtcNow
            });
            await outbox.AddAsync(
                LogisticsOutboxMessage.Create(IntegrationConstants.LogisticsEvents.WebhookNotification, webhookPayload),
                ct);
        }

        // Enqueue fiscal emission — processed asynchronously by LogisticsFiscalDispatcher
        var fiscalPayload = JsonSerializer.Serialize(new
        {
            dispatchId = dispatch.Id,
            shipmentOrderId = order.Id
        });
        await outbox.AddAsync(
            LogisticsOutboxMessage.Create(IntegrationConstants.LogisticsEvents.FiscalEmissionRequested, fiscalPayload),
            ct);

        await dispatches.SaveAsync(dispatch, ct);
    }
}

public sealed class DeliverDispatch(
    IDispatchRepository dispatches,
    ICarrierRepository carriers,
    IShipmentOrderRepository orders,
    ILogisticsOutboxRepository outbox)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var dispatch = await dispatches.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Dispatch {id} not found.");

        var carrier = await carriers.GetByIdAsync(dispatch.CarrierId, ct)
            ?? throw new KeyNotFoundException($"Carrier {dispatch.CarrierId} not found.");

        var order = await orders.GetByIdAsync(dispatch.ShipmentOrderId, ct);

        dispatch.Deliver();

        if (carrier.WebhookUrl is not null)
        {
            var webhookPayload = JsonSerializer.Serialize(new
            {
                dispatchId = dispatch.Id,
                trackingCode = dispatch.TrackingCode,
                shipmentOrderId = dispatch.ShipmentOrderId,
                orderNumber = order?.OrderNumber,
                newStatus = "Delivered",
                webhookUrl = carrier.WebhookUrl,
                carrierName = carrier.Name,
                occurredAt = DateTimeOffset.UtcNow
            });
            await outbox.AddAsync(
                LogisticsOutboxMessage.Create(IntegrationConstants.LogisticsEvents.WebhookNotification, webhookPayload),
                ct);
        }

        await dispatches.SaveAsync(dispatch, ct);
    }
}

public sealed class GetDispatchByTrackingCode(IDispatchRepository dispatches, ICarrierRepository carriers,
    IShipmentOrderRepository orders)
{
    public async Task<object?> ExecuteAsync(string trackingCode, CancellationToken ct)
    {
        var dispatch = await dispatches.GetByTrackingCodeAsync(trackingCode, ct);
        if (dispatch is null) return null;

        var carrier = await carriers.GetByIdAsync(dispatch.CarrierId, ct);
        var order = await orders.GetByIdAsync(dispatch.ShipmentOrderId, ct);

        return new
        {
            dispatch.TrackingCode,
            Status = dispatch.Status.ToString(),
            ShipmentOrderNumber = order?.OrderNumber,
            CarrierName = carrier?.Name,
            dispatch.ConferencedAt,
            dispatch.DispatchedAt,
            dispatch.DeliveredAt
        };
    }
}
