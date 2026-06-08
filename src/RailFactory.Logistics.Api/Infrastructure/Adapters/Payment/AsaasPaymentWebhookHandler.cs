using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

public sealed class AsaasPaymentWebhookHandler(
    LogisticsDbContext db,
    ILogger<AsaasPaymentWebhookHandler> logger) : IInboundWebhookHandler
{
    public string Provider => "asaas";

    public async Task HandleAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;

        // Asaas payload: { "event": "PAYMENT_RECEIVED", "payment": { "id": "pay_xxx", "status": "RECEIVED", "externalReference": "RF-..." } }
        var eventName = root.TryGetProperty("event", out var evProp) ? evProp.GetString() : null;
        if (string.IsNullOrEmpty(eventName))
        {
            logger.LogWarning("Asaas webhook missing 'event' field. EventId={EventId}", webhookEvent.Id);
            return;
        }

        if (!root.TryGetProperty("payment", out var payment))
        {
            logger.LogWarning("Asaas webhook missing 'payment' field. EventId={EventId}", webhookEvent.Id);
            return;
        }

        var externalId = payment.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogWarning("Asaas webhook 'payment.id' missing. EventId={EventId}", webhookEvent.Id);
            return;
        }

        var status = payment.TryGetProperty("status", out var stProp) ? stProp.GetString() ?? eventName : eventName;
        var boletoUrl = payment.TryGetProperty("bankSlipUrl", out var buProp) ? buProp.GetString() : null;
        var pixUrl = payment.TryGetProperty("pixQrCodeUrl", out var puProp) ? puProp.GetString() : null;

        var dispatch = await db.Dispatches
            .FirstOrDefaultAsync(d => d.PaymentExternalId == externalId, cancellationToken);

        if (dispatch is null)
        {
            logger.LogWarning(
                "No dispatch with PaymentExternalId='{ExternalId}' found for Asaas webhook.",
                externalId);
            return;
        }

        dispatch.UpdatePaymentStatus(externalId, status, boletoUrl, pixUrl);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dispatch {DispatchId} payment status updated to '{Status}' via Asaas webhook.",
            dispatch.Id, status);
    }
}
