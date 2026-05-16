using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
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
    private const string ApiGroup = "/api/inventory";
    private const string InfoPath = "/info";
    private const string BalancesPath = "/balances";
    private const string BalanceDetailsPath = "/balances/{id:guid}";
    private const string MaterialsPath = "/materials";
    private const string MaterialDetailsPath = "/materials/{materialCode}";
    private const string MaterialSuggestionsPath = "/materials/suggestions";
    private const string MaterialSearchPath = "/materials/search";
    private const string MaterialImagePath = "/materials/{materialCode}/image";
    private const string MaterialMergePath = "/materials/merge";
    
    private const string InternalPath = "/internal";
    private const string InternalCreateMaterialPath = "/materials/create";
    private const string IntegrationActor = "inventory-integration@railfactory.local";

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        // Root redirect
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}{InfoPath}"));

        var group = app.MapGroup(ApiGroup);

        group.MapGet(InfoPath, HandleGetInventoryInfo).AllowAnonymous();
        
        // SECURE PUBLIC API
        var secureGroup = group.MapGroup("/").RequireAuthorization();

        secureGroup.MapGet(BalancesPath, HandleListBalances)
            .RequirePermission(SystemPermissions.Inventory.Read);

        secureGroup.MapGet(BalanceDetailsPath, HandleGetBalanceDetails)
            .RequirePermission(SystemPermissions.Inventory.Read);
        
        secureGroup.MapPost(MaterialsPath, HandleCreateMaterial)
            .RequirePermission(SystemPermissions.Inventory.Write);
        
        secureGroup.MapGet(MaterialSearchPath, HandleSearchMaterials)
            .RequirePermission(SystemPermissions.Inventory.Read);
        
        secureGroup.MapGet(MaterialSuggestionsPath, HandleGetMaterialSuggestions)
            .RequirePermission(SystemPermissions.Inventory.Read);
        
        secureGroup.MapPut(MaterialImagePath, HandleUpdateMaterialImage)
            .RequirePermission(SystemPermissions.Inventory.Write);
        
        secureGroup.MapGet(MaterialDetailsPath, HandleGetMaterialDetails)
            .RequirePermission(SystemPermissions.Inventory.Read);

        secureGroup.MapPost(MaterialMergePath, HandleMergeMaterials)
            .RequirePermission(SystemPermissions.Inventory.Write);

        // INTERNAL API (Service-to-Service)
        var internalGroup = group.MapGroup(InternalPath);
        internalGroup.AddEndpointFilter(ValidateInternalApiKeyAsync);
        
        internalGroup.MapPost("/materials", HandleGetInternalMaterials);
        internalGroup.MapPost(InternalCreateMaterialPath, HandleCreateMaterialInternal);
        internalGroup.MapPost("/pending-balances", HandleCreatePendingBalance);
        internalGroup.MapPost("/confirmed-balances", HandleConfirmInventoryBalance);
        internalGroup.MapPost("/reserve-balances", HandleReserveInventoryBalance);
        internalGroup.MapPost("/supplier-material-mapping", HandleSupplierMaterialMappingCreated);

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

            return Results.Created($"{ApiGroup}{MaterialsPath}/{Uri.EscapeDataString(created.MaterialCode)}", created);
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

    private static async Task<IResult> HandleListBalances(
        [FromQuery] string? status,
        ListInventoryBalances listBalances,
        CancellationToken cancellationToken)
    {
        InventoryBalanceStatus? filterStatus = Enum.TryParse<InventoryBalanceStatus>(status, true, out var s) ? s : null;
        var result = await listBalances.ExecuteAsync(filterStatus, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetBalanceDetails(
        Guid id,
        GetInventoryBalanceDetails getDetails,
        CancellationToken cancellationToken)
    {
        var details = await getDetails.ExecuteAsync(id, cancellationToken);
        return details is not null ? Results.Ok(details) : Results.NotFound();
    }

    private static async Task<IResult> HandleSearchMaterials(
        [FromQuery] string q,
        SearchMaterials searchMaterials,
        CancellationToken cancellationToken)
    {
        var result = await searchMaterials.ExecuteAsync(q, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetMaterialSuggestions(
        [FromQuery] string? gtin,
        [FromQuery] string? ncm,
        [FromQuery] string? q,
        [FromQuery] string? supplierId,
        [FromQuery] string? supplierProductCode,
        GetMaterialSuggestions getSuggestions,
        CancellationToken cancellationToken)
    {
        var result = await getSuggestions.ExecuteAsync(
            new GetMaterialSuggestionsInput(gtin, ncm, q, supplierId, supplierProductCode), 
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateMaterialImage(
        string materialCode,
        [FromBody] UpdateMaterialImageRequest request,
        HttpContext context,
        UpdateMaterialImage updateImage,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        await updateImage.ExecuteAsync(MaterialCode.From(materialCode), request.ImageUrl, actor, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleGetMaterialDetails(
        string materialCode,
        GetMaterialDetails getDetails,
        CancellationToken cancellationToken)
    {
        var details = await getDetails.ExecuteAsync(MaterialCode.From(materialCode), cancellationToken);
        return details is not null ? Results.Ok(details) : Results.NotFound();
    }

    private static async Task<IResult> HandleMergeMaterials(
        [FromBody] MergeMaterialsRequest request,
        HttpContext context,
        IMergeMaterialUseCase mergeUseCase,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(actor))
        {
            return Results.Unauthorized();
        }

        try
        {
            await mergeUseCase.ExecuteAsync(
                new MergeMaterialCommand(
                    MaterialCode.From(request.ObsoleteMaterialCode),
                    MaterialCode.From(request.OfficialMaterialCode),
                    EmailAddress.From(actor)),
                cancellationToken);

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Material merge failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult HandleGetInventoryInfo(HttpContext context, IHostEnvironment environment, GetInventoryInfo getInventoryInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getInventoryInfo.Execute(
            environment.EnvironmentName,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleGetInternalMaterials(
        [FromBody] GetInternalMaterialsRequest request,
        IMaterialRepository repository,
        CancellationToken cancellationToken)
    {
        var materials = await repository.GetByCodesAsync(request.MaterialCodes, cancellationToken);
        
        var response = materials.Values.Select(m => new InternalMaterialResponse(
            m.MaterialCode.Value,
            m.OfficialName,
            m.UnitOfMeasure,
            m.ImageUrl)).ToList();
        
        return Results.Ok(response);
    }

    private static async Task<IResult> HandleCreateMaterialInternal(
        [FromBody] CreateMaterialRequest request,
        CreateMaterial createMaterial,
        CancellationToken cancellationToken)
    {
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
                IntegrationActor,
                cancellationToken);

            return Results.Created($"{ApiGroup}{InternalPath}{InternalCreateMaterialPath}/{Uri.EscapeDataString(created.MaterialCode)}", created);
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

    private static async Task<IResult> HandleCreatePendingBalance(
        [FromBody] CreatePendingBalanceRequest request,
        CreatePendingBalance createPendingBalance,
        CancellationToken cancellationToken)
    {
        await createPendingBalance.ExecuteAsync(
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

        return Results.Accepted();
    }

    private static async Task<IResult> HandleConfirmInventoryBalance(
        [FromBody] ConfirmInventoryBalanceRequest request,
        ConfirmInventoryBalance confirmBalance,
        CancellationToken cancellationToken)
    {
        await confirmBalance.ExecuteAsync(
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

        return Results.Accepted();
    }

    private static async Task<IResult> HandleReserveInventoryBalance(
        [FromBody] ReserveInventoryBalanceRequest request,
        ReserveInventoryBalance reserveBalance,
        CancellationToken cancellationToken)
    {
        try
        {
            await reserveBalance.ExecuteAsync(
                new ReserveInventoryBalanceInput(
                    request.EventId,
                    request.EventType,
                    request.CorrelationId,
                    request.Payload.ProductionOrderId,
                    request.Payload.OrderNumber,
                    request.Payload.MaterialCode,
                    request.Payload.RequiredQuantity,
                    request.Payload.UnitOfMeasure),
                cancellationToken);

            return Results.Accepted();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(title: "reservation.failed", detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> HandleSupplierMaterialMappingCreated(
        [FromBody] CreateSupplierMaterialMappingRequest request,
        RegisterSupplierMaterialMapping registerMapping,
        CancellationToken cancellationToken)
    {
        await registerMapping.ExecuteAsync(
            new RegisterSupplierMaterialMappingInput(
                request.Payload.SupplierFiscalId,
                request.Payload.SupplierProductCode,
                request.Payload.MaterialCode),
            cancellationToken);

        return Results.Accepted();
    }

    private static ValueTask<object?> ValidateInternalApiKeyAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedApiKey = configuration["InternalApiKey"];

        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return ValueTask.FromResult<object?>(Results.Problem(
                title: "Internal API key is not configured.",
                statusCode: StatusCodes.Status500InternalServerError));
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Internal-Key", out var providedApiKey)
            || !FixedTimeEquals(providedApiKey.ToString(), expectedApiKey))
        {
            return ValueTask.FromResult<object?>(Results.Unauthorized());
        }

        return next(context);
    }

    private static bool FixedTimeEquals(string providedApiKey, string expectedApiKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
