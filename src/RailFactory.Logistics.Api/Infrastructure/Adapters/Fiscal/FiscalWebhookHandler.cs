using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

public sealed class PlugNotasFiscalWebhookHandler(LogisticsDbContext db, ILogger<PlugNotasFiscalWebhookHandler> logger)
    : IInboundWebhookHandler
{
    public string Provider => "plugnotas";

    public Task HandleAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default) =>
        FiscalWebhookHandlerCore.HandleAsync(db, logger, webhookEvent, cancellationToken);
}

public sealed class FocusNfeFiscalWebhookHandler(LogisticsDbContext db, ILogger<FocusNfeFiscalWebhookHandler> logger)
    : IInboundWebhookHandler
{
    public string Provider => "focusnfe";

    public Task HandleAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default) =>
        FiscalWebhookHandlerCore.HandleAsync(db, logger, webhookEvent, cancellationToken);
}

/// <summary>Shared fiscal webhook processing logic for all fiscal providers.</summary>
internal static class FiscalWebhookHandlerCore
{
    internal static async Task HandleAsync(
        LogisticsDbContext db,
        ILogger logger,
        InboundWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(webhookEvent.Payload);
        var root = doc.RootElement;

        // PlugNotas: { "idIntegracao": "NF-RF-...", "status": "CONCLUIDO", "chave": "..." }
        // FocusNFe:  { "ref": "NF-RF-...", "status": "autorizado", "chave_nfe": "..." }
        var externalId =
            (root.TryGetProperty("idIntegracao", out var integProp) ? integProp.GetString() : null)
            ?? (root.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null);

        if (string.IsNullOrWhiteSpace(externalId))
        {
            logger.LogWarning(
                "Fiscal webhook from '{Provider}' missing correlation ID (idIntegracao/ref). EventId={EventId}",
                webhookEvent.Provider, webhookEvent.Id);
            return;
        }

        var status =
            (root.TryGetProperty("status", out var stProp) ? stProp.GetString() : null) ?? "unknown";

        // PlugNotas: "chave" | FocusNFe: "chave_nfe"
        var accessKey =
            (root.TryGetProperty("chave", out var ckProp) ? ckProp.GetString() : null)
            ?? (root.TryGetProperty("chave_nfe", out var cnfeProp) ? cnfeProp.GetString() : null);

        var dispatch = await db.Dispatches
            .FirstOrDefaultAsync(d => d.FiscalExternalId == externalId, cancellationToken);

        if (dispatch is null)
        {
            logger.LogWarning(
                "No dispatch found with FiscalExternalId='{ExternalId}' (provider '{Provider}').",
                externalId, webhookEvent.Provider);
            return;
        }

        dispatch.UpdateFiscalStatus(externalId, status, accessKey);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dispatch {DispatchId} fiscal status updated to '{Status}' via {Provider} webhook.",
            dispatch.Id, status, webhookEvent.Provider);
    }
}
