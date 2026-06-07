using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Integrations;
using RailFactory.Logistics.Api.Api.Requests;
using RailFactory.Logistics.Api.Application.Carriers;
using RailFactory.Logistics.Api.Application.Dispatches;
using RailFactory.Logistics.Api.Application.Fiscal;
using RailFactory.Logistics.Api.Application.FiscalProfiles;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Application.Shipments;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Api;

public static class LogisticsEndpoints
{
    private const string ApiGroup = "/api/logistics";

    public static WebApplication MapLogisticsEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}/info"));

        var group = app.MapGroup(ApiGroup).WithTags("Logistics");

        group.MapGet("/info", (IHostEnvironment env) =>
            Results.Ok(new { Service = "logistics", Environment = env.EnvironmentName })
        ).AllowAnonymous();

        // B2B público — sem autenticação (RD-LOG-01)
        group.MapGet("/public/dispatches/{trackingCode}", HandleGetDispatchByTracking)
            .AllowAnonymous();

        var secure = group.MapGroup("/").RequireAuthorization();

        // Carriers
        secure.MapGet("/carriers", HandleListCarriers)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapPost("/carriers", HandleCreateCarrier)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/carriers/{id:guid}/activate", HandleActivateCarrier)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/carriers/{id:guid}/deactivate", HandleDeactivateCarrier)
            .RequirePermission(SystemPermissions.Logistics.Write);

        // Shipment Orders
        secure.MapGet("/shipment-orders", HandleListShipmentOrders)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapGet("/shipment-orders/{id:guid}", HandleGetShipmentOrder)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapPost("/shipment-orders", HandleCreateShipmentOrder)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPost("/shipment-orders/{id:guid}/items", HandleAddShipmentItem)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/shipment-orders/{id:guid}/start-picking", HandleStartPicking)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/shipment-orders/{id:guid}/start-packing", HandleStartPacking)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/shipment-orders/{id:guid}/ready-to-ship", HandleMarkReadyToShip)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/shipment-orders/{id:guid}/cancel", HandleCancelShipmentOrder)
            .RequirePermission(SystemPermissions.Logistics.Write);

        // Dispatches
        secure.MapGet("/dispatches", HandleListDispatches)
            .RequirePermission(SystemPermissions.Logistics.Read);

        secure.MapGet("/dispatches/fiscal", HandleListFiscalDispatches)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapGet("/dispatches/{id:guid}", HandleGetDispatch)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapPost("/dispatches", HandleCreateDispatch)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/dispatches/{id:guid}/conference", HandleConferenceDispatch)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/dispatches/{id:guid}/ship", HandleShipDispatch)
            .RequirePermission(SystemPermissions.Logistics.Write);
        secure.MapPut("/dispatches/{id:guid}/deliver", HandleDeliverDispatch)
            .RequirePermission(SystemPermissions.Logistics.Write);

        // Heat Map (RF-35)
        secure.MapGet("/shipment-orders/heatmap", HandleGetDeliveryHeatmap)
            .RequirePermission(SystemPermissions.Logistics.Read);

        // Fiscal document emission and retry — require dedicated fiscal permission
        secure.MapPost("/dispatches/{id:guid}/fiscal-document", HandleIssueFiscalDocument)
            .RequirePermission(SystemPermissions.Logistics.Fiscal);

        secure.MapPut("/dispatches/{id:guid}/retry-fiscal", HandleRetryFiscalEmission)
            .RequirePermission(SystemPermissions.Logistics.Fiscal);

        // Tenant fiscal profile
        secure.MapGet("/fiscal-profile", HandleGetFiscalProfile)
            .RequirePermission(SystemPermissions.Logistics.Read);
        secure.MapPut("/fiscal-profile", HandleUpsertFiscalProfile)
            .RequirePermission(SystemPermissions.Logistics.Fiscal);

        // Inbound webhooks — public endpoint, no auth (providers POST here).
        // Tenant is in the URL so providers don't need to send any custom headers.
        group.MapPost("/webhooks/{provider}/{tenantCode}", HandleInboundWebhook)
            .AllowAnonymous();

        return app;
    }

    // ── Carriers ──────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListCarriers(string? status, ListCarriers useCase, CancellationToken ct)
    {
        CarrierStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarrierStatus>(status, true, out var s))
            parsedStatus = s;
        var result = await useCase.ExecuteAsync(parsedStatus, ct);
        return Results.Ok(result.Select(MapCarrierResponse));
    }

    private static async Task<IResult> HandleCreateCarrier(
        CreateCarrierRequest req, CreateCarrier useCase, CancellationToken ct)
    {
        try
        {
            var carrier = await useCase.ExecuteAsync(
                new CreateCarrierInput(req.Name, req.DocumentNumber, req.ContactEmail, req.RatePerKg, req.RatePerCbm, req.WebhookUrl), ct);
            return Results.Created($"{ApiGroup}/carriers/{carrier.Id}", MapCarrierResponse(carrier));
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
    }

    private static Task<IResult> HandleActivateCarrier(Guid id, ActivateCarrier useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleDeactivateCarrier(Guid id, DeactivateCarrier useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    // ── Shipment Orders ───────────────────────────────────────────────────────

    private static async Task<IResult> HandleListShipmentOrders(
        string? status, ListShipmentOrders useCase, CancellationToken ct)
    {
        ShipmentOrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ShipmentOrderStatus>(status, true, out var s))
            parsedStatus = s;
        var result = await useCase.ExecuteAsync(parsedStatus, ct);
        return Results.Ok(result.Select(MapShipmentOrderResponse));
    }

    private static async Task<IResult> HandleGetShipmentOrder(
        Guid id, IShipmentOrderRepository repo, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(id, ct);
        return order is null ? Results.NotFound() : Results.Ok(MapShipmentOrderResponse(order));
    }

    private static async Task<IResult> HandleCreateShipmentOrder(
        CreateShipmentOrderRequest req, CreateShipmentOrder useCase, CancellationToken ct)
    {
        var order = await useCase.ExecuteAsync(new CreateShipmentOrderInput(
            req.ProductionOrderRef, req.Notes,
            req.DeliveryLatitudeDeg, req.DeliveryLongitudeDeg, req.DeliveryCity,
            req.RecipientCnpj, req.RecipientName, req.RecipientEmail,
            req.RecipientStreet, req.RecipientNumber, req.RecipientDistrict,
            req.RecipientCity, req.RecipientState, req.RecipientZipCode,
            req.NatureOfOperation), ct);
        return Results.Created($"{ApiGroup}/shipment-orders/{order.Id}", MapShipmentOrderResponse(order));
    }

    private static async Task<IResult> HandleAddShipmentItem(
        Guid id, AddShipmentItemRequest req, AddShipmentItem useCase, CancellationToken ct)
    {
        try
        {
            var item = await useCase.ExecuteAsync(
                new AddShipmentItemInput(id, req.MaterialCode, req.Quantity, req.UnitOfMeasure,
                    req.WeightKg, req.VolumeCbm, req.NcmCode, req.CfopCode,
                    req.UnitValue, req.TaxBaseIcms, req.IcmsRate,
                    req.IcmsOrigin, req.IcmsCst, req.PisCst, req.CofinsCst, req.IpiRate), ct);
            return Results.Created($"{ApiGroup}/shipment-orders/{id}/items/{item.Id}",
                new { item.Id, item.MaterialCode, item.Quantity, item.UnitOfMeasure,
                    item.WeightKg, item.VolumeCbm, item.NcmCode, item.CfopCode, item.UnitValue,
                    item.TaxBaseIcms, item.IcmsRate, item.IcmsOrigin, item.IcmsCst, item.PisCst, item.CofinsCst, item.IpiRate });
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static Task<IResult> HandleStartPicking(Guid id, StartPicking useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleStartPacking(Guid id, StartPacking useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleMarkReadyToShip(Guid id, MarkReadyToShip useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleCancelShipmentOrder(Guid id, CancelShipmentOrder useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    // ── Dispatches ────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListDispatches(
        IDispatchRepository repo,
        CancellationToken ct,
        int page = 1,
        int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var dispatches = await repo.ListAsync(page, pageSize, ct);
        return Results.Ok(dispatches.Select(MapDispatchResponse));
    }

    private static async Task<IResult> HandleListFiscalDispatches(
        IDispatchRepository repo,
        HttpContext ctx,
        CancellationToken ct,
        int page = 1,
        int pageSize = 30)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var statuses = ctx.Request.Query["status"]
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToList();
        var (items, total) = await repo.ListFiscalAsync(page, pageSize, statuses, ct);
        return Results.Ok(new
        {
            items = items.Select(MapDispatchResponse),
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    private static async Task<IResult> HandleGetDispatch(Guid id, IDispatchRepository repo, CancellationToken ct)
    {
        var dispatch = await repo.GetByIdAsync(id, ct);
        return dispatch is null ? Results.NotFound() : Results.Ok(MapDispatchResponse(dispatch));
    }

    private static async Task<IResult> HandleCreateDispatch(
        CreateDispatchRequest req, CreateDispatch useCase, CancellationToken ct)
    {
        try
        {
            var dispatch = await useCase.ExecuteAsync(
                new CreateDispatchInput(req.ShipmentOrderId, req.CarrierId, req.VehicleId, req.DriverPersonId,
                    req.VehiclePlate, req.VehicleRntrc, req.DriverCpf, req.DriverName), ct);
            return Results.Created($"{ApiGroup}/dispatches/{dispatch.Id}", MapDispatchResponse(dispatch));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static Task<IResult> HandleConferenceDispatch(Guid id, ConferenceDispatch useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleShipDispatch(Guid id, ShipDispatch useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static Task<IResult> HandleDeliverDispatch(Guid id, DeliverDispatch useCase, CancellationToken ct)
        => ExecuteTransitionAsync(id, useCase.ExecuteAsync, ct);

    private static async Task<IResult> HandleGetDispatchByTracking(
        string trackingCode, GetDispatchByTrackingCode useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(trackingCode, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<IResult> ExecuteTransitionAsync(
        Guid id, Func<Guid, CancellationToken, Task> action, CancellationToken ct)
    {
        try { await action(id, ct); return Results.NoContent(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static object MapCarrierResponse(Carrier c) => new
    {
        c.Id, c.Name, c.DocumentNumber, c.ContactEmail, c.WebhookUrl,
        c.RatePerKg, c.RatePerCbm,
        Status = c.Status.ToString(),
        c.CreatedAt, c.UpdatedAt
    };

    // ── Heat Map (RF-35) ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleGetDeliveryHeatmap(
        IShipmentOrderRepository repo, CancellationToken ct)
    {
        var orders = await repo.ListAsync(null, ct);
        var points = orders
            .Where(o => o.DeliveryLatitudeDeg.HasValue && o.DeliveryLongitudeDeg.HasValue)
            .GroupBy(o => new { o.DeliveryCity, Lat = Math.Round((double)o.DeliveryLatitudeDeg!.Value, 2), Lon = Math.Round((double)o.DeliveryLongitudeDeg!.Value, 2) })
            .Select(g => new
            {
                g.Key.DeliveryCity,
                Lat = (decimal)g.Key.Lat,
                Lon = (decimal)g.Key.Lon,
                Count = g.Count(),
                ShippedCount = g.Count(o => o.Status == ShipmentOrderStatus.Shipped)
            })
            .OrderByDescending(p => p.Count)
            .ToList();

        return Results.Ok(new { points });
    }

    private static object MapShipmentOrderResponse(ShipmentOrder o) => new
    {
        o.Id, o.OrderNumber, o.ProductionOrderRef, o.Notes,
        Status = o.Status.ToString(),
        o.DeliveryLatitudeDeg, o.DeliveryLongitudeDeg, o.DeliveryCity,
        o.RecipientCnpj, o.RecipientName, o.RecipientEmail,
        o.RecipientStreet, o.RecipientNumber, o.RecipientDistrict,
        o.RecipientCity, o.RecipientState, o.RecipientZipCode,
        o.NatureOfOperation,
        o.CreatedAt, o.UpdatedAt,
        Items = o.Items.Select(i => new
        {
            i.Id, i.MaterialCode, i.Quantity, i.UnitOfMeasure, i.WeightKg, i.VolumeCbm,
            i.NcmCode, i.CfopCode, i.UnitValue, i.TaxBaseIcms, i.IcmsRate,
            i.IcmsOrigin, i.IcmsCst, i.PisCst, i.CofinsCst, i.IpiRate
        })
    };

    private static object MapDispatchResponse(Dispatch d) => new
    {
        d.Id, d.ShipmentOrderId, d.CarrierId, d.VehicleId, d.DriverPersonId,
        d.TrackingCode, d.FreightValueBrl,
        Status = d.Status.ToString(),
        d.FiscalExternalId, d.FiscalStatus, d.FiscalAccessKey, d.FiscalErrorMessage,
        d.MdfeExternalId, d.MdfeStatus, d.MdfeAccessKey, d.MdfeErrorMessage,
        d.VehiclePlate, d.VehicleRntrc, d.DriverCpf, d.DriverName,
        d.ConferencedAt, d.DispatchedAt, d.DeliveredAt, d.CreatedAt
    };

    // ── Fiscal Documents ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleIssueFiscalDocument(
        Guid id,
        [FromBody] IssueFiscalDocumentRequest req,
        IssueFiscalDocument useCase,
        CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(req with { DispatchId = id }, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.Code.EndsWith("not_found", StringComparison.Ordinal)
                ? Results.NotFound(new { error = result.Error.Message })
                : Results.BadRequest(new { error = result.Error.Message });
    }

    private static async Task<IResult> HandleRetryFiscalEmission(
        Guid id,
        RetryFiscalEmission useCase,
        CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.Accepted();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    // ── Inbound Webhooks ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleInboundWebhook(
        string provider,
        string tenantCode,
        HttpRequest httpRequest,
        [FromServices] IEnumerable<IWebhookSignatureValidator> validators,
        [FromServices] IInboundWebhookEventRepository repo,
        [FromServices] ITenantIntegrationClient integrationClient,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Webhooks");

        // Read body as raw string for signature validation
        using var reader = new StreamReader(httpRequest.Body);
        var rawBody = await reader.ReadToEndAsync(ct);

        // Resolve registered validator for this provider — fail-closed if unknown.
        var validator = validators.FirstOrDefault(v =>
            v.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (validator is null)
        {
            logger.LogWarning(
                "No signature validator registered for provider '{Provider}'. Request rejected.", provider);
            return Results.Problem(
                title: "Unknown provider",
                detail: $"No webhook integration registered for provider '{provider}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Fetch stored credentials for this tenant's fiscal integration to get the webhook secret.
        using var config = await integrationClient.GetConfigAsync(tenantCode, "fiscal", ct);
        if (config is null)
        {
            logger.LogWarning(
                "No fiscal integration configured for tenant {TenantCode}. Webhook rejected.", tenantCode);
            return Results.Unauthorized();
        }

        var storedSecret = config.Credentials.TryGetString(validator.CredentialKey, out var secret)
            ? secret
            : string.Empty;

        if (!validator.IsValid(rawBody, httpRequest, storedSecret))
        {
            logger.LogWarning(
                "Invalid signature from provider '{Provider}' for tenant {TenantCode}.", provider, tenantCode);
            return Results.Unauthorized();
        }

        // Determine event type and external ID from the payload.
        // Fallback: SHA256 hash of the raw body — deterministic, so identical payloads
        // from the same provider are always treated as duplicates (idempotency preserved).
        string eventType = "unknown";
        string externalId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody)));
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("event", out var evProp)) eventType = evProp.GetString() ?? eventType;
            else if (root.TryGetProperty("status", out var stProp)) eventType = stProp.GetString() ?? eventType;

            // PlugNotas: "idIntegracao" | FocusNFe: "ref" | fallback: "id"
            if (root.TryGetProperty("idIntegracao", out var integProp)) externalId = integProp.GetString() ?? externalId;
            else if (root.TryGetProperty("ref", out var refProp)) externalId = refProp.GetString() ?? externalId;
            else if (root.TryGetProperty("id", out var idProp)) externalId = idProp.GetString() ?? externalId;
        }
        catch (JsonException) { /* malformed body — hash-based externalId ensures idempotency */ }

        // Idempotency check
        if (await repo.ExistsAsync(provider, externalId, ct))
            return Results.Ok(new { status = "duplicate", message = "Already received." });

        var evt = InboundWebhookEvent.Receive(tenantCode, provider, eventType, externalId, rawBody);
        await repo.AddAsync(evt, ct);

        logger.LogInformation(
            "Inbound webhook received from {Provider} for tenant {TenantCode}: event={EventType} id={ExternalId}",
            provider, tenantCode, eventType, externalId);

        return Results.Ok(new { status = "accepted" });
    }

    // ── Fiscal Profile ────────────────────────────────────────────────────────

    private static async Task<IResult> HandleGetFiscalProfile(GetFiscalProfile useCase, CancellationToken ct)
    {
        var profile = await useCase.ExecuteAsync(ct);
        if (profile is null) return Results.NoContent();
        return Results.Ok(MapFiscalProfileResponse(profile));
    }

    private static async Task<IResult> HandleUpsertFiscalProfile(
        UpsertFiscalProfileRequest req, UpsertFiscalProfile useCase, CancellationToken ct)
    {
        var profile = await useCase.ExecuteAsync(new UpsertFiscalProfileInput(
            req.CfopPadraoIntraestadual, req.CfopPadraoInterestadual, req.UfOrigem,
            req.IcmsRate, req.IcmsCst, req.PisCst, req.CofinsCst,
            req.IpiRate, req.IcmsOrigin), ct);
        return Results.Ok(MapFiscalProfileResponse(profile));
    }

    private static object MapFiscalProfileResponse(Domain.TenantFiscalProfile p) => new
    {
        p.CfopPadraoIntraestadual,
        p.CfopPadraoInterestadual,
        p.UfOrigem,
        p.IcmsRate,
        p.IcmsCst,
        p.PisCst,
        p.CofinsCst,
        p.IpiRate,
        p.IcmsOrigin,
        p.UpdatedAt,
    };
}
