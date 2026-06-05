using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Fiscal;

public sealed class RetryFiscalEmission(
    IDispatchRepository dispatches,
    ILogisticsOutboxRepository outbox)
{
    public async Task ExecuteAsync(Guid dispatchId, CancellationToken ct)
    {
        var dispatch = await dispatches.GetByIdAsync(dispatchId, ct)
            ?? throw new KeyNotFoundException($"Dispatch {dispatchId} not found.");

        dispatch.RequestFiscalRetry();

        var payload = JsonSerializer.Serialize(new
        {
            dispatchId = dispatch.Id,
            shipmentOrderId = dispatch.ShipmentOrderId,
        });

        await outbox.AddAsync(
            LogisticsOutboxMessage.Create(IntegrationConstants.LogisticsEvents.FiscalEmissionRequested, payload),
            ct);

        await dispatches.SaveAsync(dispatch, ct);
    }
}
