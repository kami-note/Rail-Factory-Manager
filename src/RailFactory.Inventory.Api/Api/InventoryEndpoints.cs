using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Api.Requests;
using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Materials;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Api;

/// <summary>
/// Defines the API routes and handlers for the Inventory module.
/// </summary>
public static class InventoryEndpoints
{
    private const string InventoryInfoPath = "/api/inventory/info";
    private const string BalanceDetailsPath = "/api/inventory/balances/{id:guid}";
    private const string BalancesPath = "/api/inventory/balances";
    private const string MaterialsPath = "/api/inventory/materials";
    private const string MaterialDetailsPath = "/api/inventory/materials/{materialCode}";
    private const string MaterialSuggestionsPath = "/api/inventory/materials/suggestions";
    private const string InternalPendingBalancesPath = "/internal/pending-balances";
    private const string InternalConfirmedBalancesPath = "/internal/confirmed-balances";
    private const string InternalSupplierMaterialMappingPath = "/internal/supplier-material-mapping";

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet(InventoryInfoPath, HandleGetInventoryInfo);
        app.MapGet(BalanceDetailsPath, HandleGetBalanceDetails);
        app.MapGet(BalancesPath, HandleListBalances);
        app.MapPost(MaterialsPath, HandleCreateMaterial);
        app.MapGet("/api/inventory/materials/search", HandleSearchMaterials);
        app.MapGet(MaterialSuggestionsPath, HandleGetMaterialSuggestions);
        app.MapPut("/api/inventory/materials/{materialCode}/image", HandleUpdateMaterialImage);
        app.MapGet(MaterialDetailsPath, HandleGetMaterialDetails);
        app.MapPost("/internal/materials", HandleGetInternalMaterials);
        app.MapPost(InternalPendingBalancesPath, HandleCreatePendingBalance);
        app.MapPost(InternalConfirmedBalancesPath, HandleConfirmInventoryBalance);
        app.MapPost(InternalSupplierMaterialMappingPath, HandleSupplierMaterialMappingCreated);

        return app;
    }

    private static async Task<IResult> HandleCreateMaterial(
        [FromBody] CreateMaterialRequest request,
        HttpContext context,
        CreateMaterial createMaterial,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(actor))
        {
            return Results.Unauthorized();
        }

        try
        {
            var created = await createMaterial.ExecuteAsync(
                new CreateMaterialInput(
                    request.MaterialCode,
                    request.OfficialName,
                    request.Description,
                    request.UnitOfMeasure,
                    request.ProcurementType,
                    request.Category,
                    request.Gtin,
                    request.Ncm),
                actor,
                cancellationToken);

            return Results.Created($"{MaterialsPath}/{Uri.EscapeDataString(created.MaterialCode)}", created);
        }
        catch (MaterialValidationException ex)
        {
            var statusCode = ex.Code is "material.duplicate_code" or "material.duplicate_gtin"
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "Invalid material request",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static async Task<IResult> HandleGetMaterialDetails(
        [FromRoute] string materialCode,
        GetMaterialDetails getMaterialDetails,
        CancellationToken cancellationToken)
    {
        var details = await getMaterialDetails.ExecuteAsync(materialCode, cancellationToken);
        return details is not null ? Results.Ok(details) : Results.NotFound();
    }

    private static async Task<IResult> HandleSearchMaterials(
        [FromQuery] string q,
        SearchMaterials searchMaterials,
        CancellationToken cancellationToken)
    {
        var results = await searchMaterials.ExecuteAsync(q, cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult> HandleGetMaterialSuggestions(
        [FromQuery] string? gtin,
        [FromQuery] string? ncm,
        [FromQuery] string? description,
        [FromQuery] string? supplierFiscalId,
        [FromQuery] string? supplierProductCode,
        GetMaterialSuggestions getMaterialSuggestions,
        CancellationToken cancellationToken)
    {
        var input = new GetMaterialSuggestionsInput(gtin, ncm, description, supplierFiscalId, supplierProductCode);
        var results = await getMaterialSuggestions.ExecuteAsync(input, cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult> HandleSupplierMaterialMappingCreated(
        [FromBody] CreateSupplierMaterialMappingRequest request,
        RegisterSupplierMaterialMapping registerSupplierMaterialMapping,
        CancellationToken cancellationToken)
    {
        var input = new RegisterSupplierMaterialMappingInput(
            request.Payload.SupplierFiscalId,
            request.Payload.SupplierProductCode,
            request.Payload.MaterialCode);

        await registerSupplierMaterialMapping.ExecuteAsync(input, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> HandleUpdateMaterialImage(
        [FromRoute] string materialCode,
        [FromBody] UpdateMaterialImageRequest request,
        HttpContext context,
        UpdateMaterialImage updateMaterialImage,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(actor))
        {
            return Results.Unauthorized();
        }

        var updated = await updateMaterialImage.ExecuteAsync(
            materialCode, 
            request.ImageUrl, 
            actor,
            cancellationToken);
        return updated ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HandleGetInternalMaterials(
        [FromBody] GetInternalMaterialsRequest request,
        [FromHeader(Name = "X-Internal-Key")] string? apiKey,
        IConfiguration configuration,
        IMaterialRepository repository,
        CancellationToken cancellationToken)
    {
        var expectedKey = configuration["InternalApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey) || apiKey != expectedKey)
        {
            return Results.Unauthorized();
        }

        if (request.MaterialCodes == null || !request.MaterialCodes.Any())
        {
            return Results.Ok(Enumerable.Empty<InternalMaterialResponse>());
        }

        if (request.MaterialCodes.Count() > 100)
        {
            return Results.BadRequest(new { code = "batch_too_large", message = "Maximum 100 codes per request." });
        }

        var materials = await repository.GetByCodesAsync(request.MaterialCodes, cancellationToken);
        var response = materials.Select(m => new InternalMaterialResponse(
            m.Key,
            m.Value.OfficialName,
            m.Value.ImageUrl));

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleGetInventoryInfo(
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

    private static async Task<IResult> HandleListBalances(
        [FromQuery] string? status,
        ListInventoryBalances listInventoryBalances,
        CancellationToken cancellationToken)
    {
        InventoryBalanceStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InventoryBalanceStatus>(status, true, out var result))
        {
            parsedStatus = result;
        }

        var response = await listInventoryBalances.ExecuteAsync(parsedStatus, cancellationToken);
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
