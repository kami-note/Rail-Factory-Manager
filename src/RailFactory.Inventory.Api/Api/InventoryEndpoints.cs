using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Api.Requests;
using RailFactory.Inventory.Api.Application;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Materials;

namespace RailFactory.Inventory.Api.Api;

/// <summary>
/// Defines the API routes and handlers for the Inventory module.
/// </summary>
public static class InventoryEndpoints
{
    private const string InventoryInfoPath = "/api/inventory/info";
    private const string BalanceDetailsPath = "/api/inventory/balances/{id:guid}";
    private const string PendingBalancesPath = "/api/inventory/balances/pending";
    private const string InternalPendingBalancesPath = "/internal/pending-balances";
    private const string InternalConfirmedBalancesPath = "/internal/confirmed-balances";

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet(InventoryInfoPath, HandleGetInventoryInfo);
        app.MapGet(BalanceDetailsPath, HandleGetBalanceDetails);
        app.MapGet(PendingBalancesPath, HandleListPendingBalances);
        app.MapPut("/api/inventory/materials/{materialCode}/image", HandleUpdateMaterialImage);
        app.MapPost(InternalPendingBalancesPath, HandleCreatePendingBalance);
        app.MapPost(InternalConfirmedBalancesPath, HandleConfirmInventoryBalance);

        return app;
    }

    private static async Task<IResult> HandleUpdateMaterialImage(
        [FromRoute] string materialCode,
        [FromBody] UpdateMaterialImageRequest request,
        UpdateMaterialImage updateMaterialImage,
        CancellationToken cancellationToken)
    {
        var updated = await updateMaterialImage.ExecuteAsync(materialCode, request.ImageUrl, cancellationToken);
        return updated ? Results.NoContent() : Results.NotFound();
    }

    private static IResult HandleGetInventoryInfo(
        [FromServices] IWebHostEnvironment env,
        [FromServices] ITenantContextAccessor tenantContext,
        GetInventoryInfo getInventoryInfo)
    {
        var info = getInventoryInfo.Execute(
            env.EnvironmentName,
            tenantContext.Current?.Locale,
            tenantContext.Current?.TimeZone);
        return Results.Ok(info);
    }

    private static async Task<IResult> HandleGetBalanceDetails(
        [FromRoute] Guid id,
        GetInventoryBalanceDetails getInventoryBalanceDetails,
        CancellationToken cancellationToken)
    {
        var details = await getInventoryBalanceDetails.ExecuteAsync(id, cancellationToken);
        return details is not null ? Results.Ok(details) : Results.NotFound();
    }

    private static async Task<IResult> HandleListPendingBalances(
        ListPendingBalances listPendingBalances,
        CancellationToken cancellationToken)
    {
        var response = await listPendingBalances.ExecuteAsync(cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> HandleCreatePendingBalance(
        [FromBody] CreatePendingBalanceRequest request,
        CreatePendingBalance createPendingBalance,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await createPendingBalance.ExecuteAsync(
                new CreatePendingBalanceInput(
                    request.EventId,
                    request.EventType,
                    request.CorrelationId,
                    request.Payload.ReceiptId,
                    request.Payload.ReceiptItemId,
                    request.Payload.ReceiptNumber,
                    request.Payload.MaterialCode,
                    request.Payload.Quantity,
                    request.Payload.UnitOfMeasure,
                    request.Payload.UnitPrice,
                    request.Payload.OriginalDescription,
                    request.Payload.AccessKey,
                    request.Payload.SupplierName,
                    request.Payload.Source,
                    request.Payload.Ncm,
                    request.Payload.Gtin),
                cancellationToken);

            return created ? Results.Accepted() : Results.Ok(new { status = "duplicate_ignored" });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Failed to create pending balance",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "integration.invalid_payload" });
        }
    }

    private static async Task<IResult> HandleConfirmInventoryBalance(
        [FromBody] ConfirmInventoryBalanceRequest request,
        ConfirmInventoryBalance confirmInventoryBalance,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmed = await confirmInventoryBalance.ExecuteAsync(
                new ConfirmInventoryBalanceInput(
                    request.EventId,
                    request.EventType,
                    request.CorrelationId,
                    request.Payload.ReceiptId,
                    request.Payload.ReceiptItemId,
                    request.Payload.Status,
                    request.Payload.IsApproved,
                    request.Payload.CountedQuantity,
                    request.Payload.LotNumber,
                    request.Payload.ExpirationDate),
                cancellationToken);

            return confirmed ? Results.Accepted() : Results.Ok(new { status = "duplicate_ignored" });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Failed to confirm balance",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "integration.invalid_payload" });
        }
    }
}
