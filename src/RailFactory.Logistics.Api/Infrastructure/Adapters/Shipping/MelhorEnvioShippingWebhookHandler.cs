using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

public sealed class MelhorEnvioShippingWebhookHandler(
    LogisticsDbContext db,
    ILogger<MelhorEnvioShippingWebhookHandler> logger) : IInboundWebhookHandler
{
    public string Provider => "melhorenvio";

    public async Task HandleAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;

        // ME payload: { "event": "order.posted", "data": { "id": "uuid", "status": "posted", "tracking": "..." } }
        var meEvent = root.TryGetProperty("event", out var evProp) ? evProp.GetString() : null;
        if (string.IsNullOrEmpty(meEvent))
        {
            logger.LogWarning("Melhor Envio webhook missing 'event' field. EventId={EventId}", webhookEvent.Id);
            return;
        }

        if (!root.TryGetProperty("data", out var data))
        {
            logger.LogWarning("Melhor Envio webhook missing 'data' field. EventId={EventId}", webhookEvent.Id);
            return;
        }

        var externalId = data.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogWarning("Melhor Envio webhook 'data.id' missing. EventId={EventId}", webhookEvent.Id);
            return;
        }

        var tracking = data.TryGetProperty("tracking", out var trProp) ? trProp.GetString() : null;

        var dispatch = await db.Dispatches
            .FirstOrDefaultAsync(d => d.ShippingExternalId == externalId, cancellationToken);

        if (dispatch is null)
        {
            logger.LogWarning(
                "No dispatch with ShippingExternalId='{ExternalId}' found for ME webhook.",
                externalId);
            return;
        }

        dispatch.UpdateShippingStatus(externalId, meEvent, dispatch.ShippingLabelUrl, null);

        // Auto-deliver when ME confirms delivery
        if (meEvent == "order.delivered" && dispatch.Status == DispatchStatus.InTransit)
        {
            dispatch.Deliver();
            logger.LogInformation(
                "Dispatch {DispatchId} auto-delivered via Melhor Envio webhook.", dispatch.Id);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dispatch {DispatchId} shipping status updated to '{Event}' via Melhor Envio webhook.",
            dispatch.Id, meEvent);
    }
}
