using RailFactory.BuildingBlocks.Auth;
using RailFactory.Logistics.Api.Api.Requests;
using RailFactory.Logistics.Api.Application.Carriers;
using RailFactory.Logistics.Api.Application.Dispatches;
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

    private static async Task<IResult> HandleActivateCarrier(Guid id, ActivateCarrier useCase, CancellationToken ct)
    {
        try { await useCase.ExecuteAsync(id, ct); return Results.NoContent(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleDeactivateCarrier(Guid id, DeactivateCarrier useCase, CancellationToken ct)
    {
        try { await useCase.ExecuteAsync(id, ct); return Results.NoContent(); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
    }

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
        var order = await useCase.ExecuteAsync(new CreateShipmentOrderInput(req.ProductionOrderRef, req.Notes), ct);
        return Results.Created($"{ApiGroup}/shipment-orders/{order.Id}", MapShipmentOrderResponse(order));
    }

    private static async Task<IResult> HandleAddShipmentItem(
        Guid id, AddShipmentItemRequest req, AddShipmentItem useCase, CancellationToken ct)
    {
        try
        {
            var item = await useCase.ExecuteAsync(
                new AddShipmentItemInput(id, req.MaterialCode, req.Quantity, req.UnitOfMeasure, req.WeightKg, req.VolumeCbm), ct);
            return Results.Created($"{ApiGroup}/shipment-orders/{id}/items/{item.Id}",
                new { item.Id, item.MaterialCode, item.Quantity, item.UnitOfMeasure, item.WeightKg, item.VolumeCbm });
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
                new CreateDispatchInput(req.ShipmentOrderId, req.CarrierId, req.VehicleId, req.DriverPersonId), ct);
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

    private static object MapShipmentOrderResponse(ShipmentOrder o) => new
    {
        o.Id, o.OrderNumber, o.ProductionOrderRef, o.Notes,
        Status = o.Status.ToString(),
        o.CreatedAt, o.UpdatedAt,
        Items = o.Items.Select(i => new
        {
            i.Id, i.MaterialCode, i.Quantity, i.UnitOfMeasure, i.WeightKg, i.VolumeCbm
        })
    };

    private static object MapDispatchResponse(Dispatch d) => new
    {
        d.Id, d.ShipmentOrderId, d.CarrierId, d.VehicleId, d.DriverPersonId,
        d.TrackingCode, d.FreightValueBrl,
        Status = d.Status.ToString(),
        d.ConferencedAt, d.DispatchedAt, d.DeliveredAt, d.CreatedAt
    };
}
