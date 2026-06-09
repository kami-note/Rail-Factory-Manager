using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Api.Requests;
using RailFactory.Production.Api.Application;
using RailFactory.Production.Api.Application.Boms;
using RailFactory.Production.Api.Application.Orders;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Application.WorkCenters;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Api;

/// <summary>
/// Defines the API routes and handlers for the Production module.
/// </summary>
public static class ProductionEndpoints
{
    private const string ApiGroup = "/api/production";

    public static WebApplication MapProductionEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}/info"));

        var group = app.MapGroup(ApiGroup).WithTags("Production");

        group.MapGet("/info", (HttpContext ctx, IHostEnvironment env, GetProductionInfo info) =>
        {
            var tenant = ctx.GetResolvedTenant();
            return Results.Ok(info.Execute(env.EnvironmentName, tenant?.Locale, tenant?.TimeZone));
        }).AllowAnonymous();

        var secure = group.MapGroup("/").RequireAuthorization();

        // Work Centers
        secure.MapGet("/work-centers", HandleListWorkCenters)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapGet("/work-centers/{id:guid}", HandleGetWorkCenter)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapPost("/work-centers", HandleCreateWorkCenter)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/work-centers/{id:guid}/deactivate", HandleDeactivateWorkCenter)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/work-centers/{id:guid}/activate", HandleActivateWorkCenter)
            .RequirePermission(SystemPermissions.Production.Write);

        // BOMs
        secure.MapGet("/boms", HandleListBoms)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapGet("/boms/{id:guid}", HandleGetBom)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapPost("/boms", HandleCreateBom)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPost("/boms/{id:guid}/items", HandleAddBomItem)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/boms/{id:guid}/activate", HandleActivateBom)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPost("/boms/{id:guid}/clone", HandleCloneBom)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapGet("/boms/{id:guid}/cost-rollup", HandleGetBomCostRollup)
            .RequirePermission(SystemPermissions.Production.Read);

        // Production Orders
        secure.MapGet("/production-orders", HandleListOrders)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapGet("/production-orders/{id:guid}", HandleGetOrder)
            .RequirePermission(SystemPermissions.Production.Read);

        secure.MapPost("/production-orders", HandleCreateOrder)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/production-orders/{id:guid}/release", HandleReleaseOrder)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/production-orders/{id:guid}/start-execution", HandleStartExecution)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/production-orders/{id:guid}/cancel", HandleCancelOrder)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPost("/production-orders/{id:guid}/consumptions", HandleRecordConsumption)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPost("/production-orders/{id:guid}/scraps", HandleRecordScrap)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPost("/production-orders/{id:guid}/inspections", HandleRecordInspection)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapPut("/production-orders/{id:guid}/complete", HandleCompleteOrder)
            .RequirePermission(SystemPermissions.Production.Write);

        secure.MapGet("/production-orders/{id:guid}/execution", HandleGetOrderExecution)
            .RequirePermission(SystemPermissions.Production.Read);

        // Dashboard
        secure.MapGet("/dashboard", HandleGetDashboard)
            .RequirePermission(SystemPermissions.Production.Read);

        return app;
    }

    // ── Work Centers ──────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListWorkCenters(
        ListWorkCenters useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(ct);
        return Results.Ok(result.Select(MapWorkCenterResponse));
    }

    private static async Task<IResult> HandleGetWorkCenter(
        Guid id, IWorkCenterRepository repo, CancellationToken ct)
    {
        var wc = await repo.GetByIdAsync(id, ct);
        return wc is null ? Results.NotFound() : Results.Ok(MapWorkCenterResponse(wc));
    }

    private static async Task<IResult> HandleCreateWorkCenter(
        CreateWorkCenterRequest req, CreateWorkCenter useCase, CancellationToken ct)
    {
        try
        {
            var wc = await useCase.ExecuteAsync(new CreateWorkCenterInput(req.Code, req.Name), ct);
            return Results.Created($"{ApiGroup}/work-centers/{wc.Id}", MapWorkCenterResponse(wc));
        }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleDeactivateWorkCenter(
        Guid id, DeactivateWorkCenter useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleActivateWorkCenter(
        Guid id, ActivateWorkCenter useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── BOMs ──────────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleListBoms(
        string? productCode, ListBoms useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(productCode, ct);
        return Results.Ok(result.Select(MapBomResponse));
    }

    private static async Task<IResult> HandleGetBom(
        Guid id, IBomRepository repo, CancellationToken ct)
    {
        var bom = await repo.GetByIdAsync(id, ct);
        return bom is null ? Results.NotFound() : Results.Ok(MapBomResponse(bom));
    }

    private static async Task<IResult> HandleCreateBom(
        CreateBomRequest req, CreateBom useCase, CancellationToken ct)
    {
        var bom = await useCase.ExecuteAsync(new CreateBomInput(req.ProductCode, req.BatchSize), ct);
        return Results.Created($"{ApiGroup}/boms/{bom.Id}", MapBomResponse(bom));
    }

    private static async Task<IResult> HandleCloneBom(
        Guid id, CloneBomVersion useCase, CancellationToken ct)
    {
        try
        {
            var newBom = await useCase.ExecuteAsync(id, ct);
            return Results.Created($"{ApiGroup}/boms/{newBom.Id}", MapBomResponse(newBom));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (ArgumentException ex) { return Results.BadRequest(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleGetBomCostRollup(
        Guid id, GetBomCostRollup useCase, CancellationToken ct)
    {
        try
        {
            var result = await useCase.ExecuteAsync(id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleAddBomItem(
        Guid id, AddBomItemRequest req, AddBomItem useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(new AddBomItemInput(id, req.MaterialCode, req.Quantity, req.UnitOfMeasure, req.ScrapFactor), ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleActivateBom(
        Guid id, ActivateBomVersion useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── Production Orders ─────────────────────────────────────────────────────

    private static async Task<IResult> HandleListOrders(
        string? status, Guid? workCenterId, string? productCode, ListProductionOrders useCase, CancellationToken ct)
    {
        ProductionOrderStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProductionOrderStatus>(status, true, out var s))
            parsedStatus = s;

        var result = await useCase.ExecuteAsync(parsedStatus, workCenterId, ct, productCode);
        return Results.Ok(result.Select(MapOrderResponse));
    }

    private static async Task<IResult> HandleGetOrder(
        Guid id, IProductionOrderRepository repo, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(id, ct);
        return order is null ? Results.NotFound() : Results.Ok(MapOrderResponse(order));
    }

    private static async Task<IResult> HandleCreateOrder(
        CreateProductionOrderRequest req, CreateProductionOrder useCase, CancellationToken ct)
    {
        try
        {
            var order = await useCase.ExecuteAsync(
                new CreateProductionOrderInput(req.BomId, req.WorkCenterId, req.PlannedQuantity), ct);
            return Results.Created($"{ApiGroup}/production-orders/{order.Id}", MapOrderResponse(order));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleReleaseOrder(
        Guid id, ReleaseProductionOrder useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleCancelOrder(
        Guid id, CancelProductionOrder useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    // ── Execution ─────────────────────────────────────────────────────────────

    private static async Task<IResult> HandleStartExecution(
        Guid id, StartOrderExecution useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleRecordConsumption(
        Guid id,
        [FromBody] RecordConsumptionRequest req,
        RecordConsumption useCase,
        CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(new RecordConsumptionInput(id, req.MaterialCode, req.ConsumedQuantity, req.UnitOfMeasure, req.InventoryBalanceId), ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleRecordScrap(
        Guid id,
        [FromBody] RecordScrapRequest req,
        RecordScrap useCase,
        CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(new RecordScrapInput(id, req.MaterialCode, req.ScrapQuantity, req.UnitOfMeasure, req.Reason), ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleRecordInspection(
        Guid id,
        [FromBody] RecordInspectionRequest req,
        RecordQualityInspection useCase,
        CancellationToken ct)
    {
        try
        {
            var result = Enum.Parse<InspectionResult>(req.Result, ignoreCase: true);
            await useCase.ExecuteAsync(new RecordQualityInspectionInput(id, result, req.InspectedBy, req.Notes), ct);
            return Results.NoContent();
        }
        catch (ArgumentException) { return Results.BadRequest(new { Error = $"Invalid inspection result. Valid values: {string.Join(", ", Enum.GetNames<InspectionResult>())}" }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleCompleteOrder(
        Guid id, CompleteProductionOrder useCase, CancellationToken ct)
    {
        try
        {
            await useCase.ExecuteAsync(id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { Error = ex.Message }); }
    }

    private static async Task<IResult> HandleGetOrderExecution(
        Guid id, GetOrderExecutionHistory useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(id, ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static object MapWorkCenterResponse(WorkCenter wc) => new
    {
        wc.Id, wc.Code, wc.Name,
        Status = wc.Status.ToDisplayStatus(),
        wc.CreatedAt, wc.UpdatedAt
    };

    private static object MapBomResponse(BillOfMaterials bom) => new
    {
        bom.Id,
        ProductCode = bom.ProductCode.Value,
        bom.Version,
        Status = bom.Status.ToDisplayStatus(),
        BatchSize = bom.BatchSize,
        Items = bom.Items.Select(i => new
        {
            i.Id,
            MaterialCode = i.MaterialCode.Value,
            i.Quantity,
            i.UnitOfMeasure,
            i.ScrapFactor
        }),
        bom.CreatedAt, bom.UpdatedAt
    };

    private static async Task<IResult> HandleGetDashboard(
        GetProductionDashboard useCase, CancellationToken ct)
    {
        var result = await useCase.ExecuteAsync(ct);
        return Results.Ok(result);
    }

    private static object MapOrderResponse(ProductionOrder order) => new
    {
        order.Id,
        order.OrderNumber,
        ProductCode = order.ProductCode.Value,
        order.BomId,
        order.WorkCenterId,
        order.PlannedQuantity,
        Status = order.Status.ToDisplayStatus(),
        order.CreatedAt, order.UpdatedAt
    };
}
