using Microsoft.AspNetCore.Mvc;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Api.Requests;
using RailFactory.SupplyChain.Api.Api.Responses;
using RailFactory.SupplyChain.Api.Api.Validation;
using RailFactory.SupplyChain.Api.Application;
using RailFactory.SupplyChain.Api.Application.Integration;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Application.Receiving;
using RailFactory.SupplyChain.Api.Application.Suppliers;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Api;

public static class SupplyChainEndpoints
{
    private const string ApiGroup = "/api/supply-chain";
    private const string InfoPath = "/info";
    private const string SuppliersPath = "/suppliers";
    private const string ReceiptsPath = "/receipts";
    private const string AssociationQueuePath = "/receipts/association-queue";
    private const string ReceiptDetailsPath = "/receipts/{id:guid}";
    private const string AssociationWorkbenchPath = "/receipts/{id:guid}/association-workbench";
    private const string ReceiptItemsToAssociatePath = "/receipts/{id:guid}/items-to-associate";
    private const string AssociateReceiptItemPath = "/receipts/{receiptId:guid}/items/{itemId:guid}/association";
    private const string CreateMaterialAndAssociatePath = "/receipts/{receiptId:guid}/items/{itemId:guid}/create-material-and-associate";
    private const string ReviewAssociationItemLaterPath = "/receipts/{receiptId:guid}/items/{itemId:guid}/review-later";
    private const string IgnoreAssociationItemPath = "/receipts/{receiptId:guid}/items/{itemId:guid}/ignored";
    private const string OverrideSupplierSkuPath = "/receipts/{receiptId:guid}/items/{itemId:guid}/override-supplier-sku";
    private const string ReleaseToConferencePath = "/receipts/{receiptId:guid}/release-to-conference";
    private const string MappingsPath = "/mappings";
    private const string ImportXmlPath = "/receipts/import/xml";
    private const string ImportXmlPreviewPath = "/receipts/import/xml/preview";
    private const string ImportXmlBatchPath = "/receipts/import/xml/batch";
    private const string ReceiptXmlPath = "/receipts/{id:guid}/xml";
    private const string StartConferencePath = "/receipts/{id:guid}/conference/start";
    private const string CloseConferencePath = "/receipts/{id:guid}/conference/close";
    private const string ConferenceItemsPath = "/receipts/{id:guid}/conference/items";
    private const string OutboxDeadLettersPath = "/outbox/dead-letters";
    private const string ReplayOutboxDeadLettersPath = "/outbox/dead-letters/replay";

    public static WebApplication MapSupplyChainEndpoints(this WebApplication app)
    {
        // Root redirect
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}{InfoPath}"));

        var group = app.MapGroup(ApiGroup);

        group.MapGet(InfoPath, HandleGetInfo).AllowAnonymous();
        
        // SECURE PUBLIC API
        var secureGroup = group.MapGroup("/").RequireAuthorization();

        secureGroup.MapPost(SuppliersPath, HandleCreateSupplier)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapGet(ReceiptsPath, HandleListReceipts)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapGet(AssociationQueuePath, HandleListAssociationQueue)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapGet(ReceiptDetailsPath, HandleGetReceiptDetails)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapGet(AssociationWorkbenchPath, HandleGetAssociationWorkbench)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapGet(ReceiptItemsToAssociatePath, HandleGetItemsToAssociate)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapPost(AssociateReceiptItemPath, HandleAssociateReceiptItem)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(CreateMaterialAndAssociatePath, HandleCreateMaterialAndAssociate)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(ReviewAssociationItemLaterPath, HandleReviewAssociationItemLater)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(IgnoreAssociationItemPath, HandleIgnoreAssociationItem)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(OverrideSupplierSkuPath, HandleOverrideSupplierSku)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(ReleaseToConferencePath, HandleReleaseToConference)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(MappingsPath, HandleCreateMapping)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapGet(ReceiptXmlPath, HandleGetReceiptXml)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapPost(StartConferencePath, HandleStartConference)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(CloseConferencePath, HandleCloseConference)
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapGet(ConferenceItemsPath, HandleGetConferenceItems)
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapPost(ImportXmlPath, HandleImportXmlReceipt)
            .DisableAntiforgery()
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapPost(ImportXmlPreviewPath, HandlePreviewXmlReceipt)
            .DisableAntiforgery()
            .RequirePermission(SystemPermissions.SupplyChain.Read);
        
        secureGroup.MapPost(ImportXmlBatchPath, HandleImportXmlReceiptBatch)
            .DisableAntiforgery()
            .RequirePermission(SystemPermissions.SupplyChain.Write);
        
        secureGroup.MapGet(OutboxDeadLettersPath, HandleListOutboxDeadLetters)
            .RequirePermission(SystemPermissions.SupplyChain.Admin);
        
        secureGroup.MapPost(ReplayOutboxDeadLettersPath, HandleReplayOutboxDeadLetters)
            .RequirePermission(SystemPermissions.SupplyChain.Admin);
            
        return app;
    }

    private static async Task<IResult> HandleGetItemsToAssociate(
        Guid id,
        GetItemsToAssociate getItems,
        CancellationToken cancellationToken)
    {
        var items = await getItems.ExecuteAsync(id, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> HandleCreateSupplier(
        [FromBody] CreateSupplierRequest request,
        CreateSupplier createSupplier,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null) return validation;

        var result = await createSupplier.ExecuteAsync(request.FiscalId, request.Name, cancellationToken);
        return Results.Created($"{ApiGroup}{SuppliersPath}/{result.Id}", result);
    }

    private static async Task<IResult> HandleListReceipts(
        ListReceipts listReceipts,
        CancellationToken cancellationToken)
    {
        var result = await listReceipts.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleListAssociationQueue(
        ListAssociationQueue listQueue,
        CancellationToken cancellationToken)
    {
        var result = await listQueue.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetReceiptDetails(
        Guid id,
        GetMaterialReceiptDetails getDetails,
        CancellationToken cancellationToken)
    {
        var result = await getDetails.ExecuteAsync(id, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleGetAssociationWorkbench(
        Guid id,
        GetAssociationWorkbench getWorkbench,
        CancellationToken cancellationToken)
    {
        var result = await getWorkbench.ExecuteAsync(id, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleAssociateReceiptItem(
        Guid receiptId,
        Guid itemId,
        [FromBody] AssociateReceiptItemRequest request,
        HttpContext context,
        AssociateReceiptItem associateItem,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await associateItem.ExecuteAsync(
                receiptId, itemId, request.ExpectedVersion, request.InternalMaterialCode, request.ConversionFactor,
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
    }

    private static async Task<IResult> HandleCreateMaterialAndAssociate(
        Guid receiptId,
        Guid itemId,
        [FromBody] CreateMaterialAndAssociateRequest request,
        HttpContext context,
        CreateMaterialAndAssociate createAndAssociate,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await createAndAssociate.ExecuteAsync(
                receiptId, itemId, request.ExpectedVersion, 
                new CreateMaterialInput(
                    request.MaterialCode, request.OfficialName, request.Description, 
                    request.UnitOfMeasure, request.ProcurementType, request.Category, 
                    request.Gtin, request.Ncm),
                request.ConversionFactor,
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
        catch (RemoteServiceValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
    }

    private static async Task<IResult> HandleReviewAssociationItemLater(
        Guid receiptId,
        Guid itemId,
        [FromBody] ControlledAssociationDecisionRequest request,
        HttpContext context,
        RecordControlledAssociationDecision reviewLater,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await reviewLater.ExecuteAsync(
                receiptId, itemId, request.ExpectedVersion, 
                ControlledAssociationDecision.ReviewLater, request.Reason, 
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
    }

    private static async Task<IResult> HandleIgnoreAssociationItem(
        Guid receiptId,
        Guid itemId,
        [FromBody] ControlledAssociationDecisionRequest request,
        HttpContext context,
        RecordControlledAssociationDecision ignoreItem,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await ignoreItem.ExecuteAsync(
                receiptId, itemId, request.ExpectedVersion, 
                ControlledAssociationDecision.Ignored, request.Reason, 
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
    }

    private static async Task<IResult> HandleOverrideSupplierSku(
        Guid receiptId,
        Guid itemId,
        [FromBody] OverrideSupplierProductCodeRequest request,
        HttpContext context,
        OverrideSupplierProductCode overrideSku,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await overrideSku.ExecuteAsync(
                receiptId, itemId, request.ExpectedVersion, 
                request.CorrectedCode, request.Reason, 
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
    }

    private static async Task<IResult> HandleReleaseToConference(
        Guid receiptId,
        [FromBody] ReleaseReceiptToConferenceRequest request,
        HttpContext context,
        ReleaseReceiptToConference releaseToConference,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await releaseToConference.ExecuteAsync(
                receiptId, request.ExpectedReceiptVersion, 
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationValidationException ex)
        {
            return ToProblemResult(ex.Code, ex.Message);
        }
        catch (AssociationConflictException ex)
        {
            return ToProblemResult("association.conflict", ex.Message);
        }
        catch (AssociationReleaseBlockedException ex)
        {
            return Results.Problem(
                title: "Release blocked",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> 
                { 
                    ["code"] = "association.release_blocked",
                    ["blockers"] = ex.Blockers
                });
        }
    }

    private static async Task<IResult> HandleCreateMapping(
        [FromBody] CreateSupplierMaterialMappingRequest request,
        HttpContext context,
        CreateSupplierMaterialMapping registerMapping,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        await registerMapping.ExecuteAsync(
            request.SupplierFiscalId, 
            request.SupplierProductCode, 
            request.InternalMaterialCode, 
            request.InternalUnitOfMeasure,
            request.SupplierUnit,
            request.ConversionFactor,
            actor,
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleGetReceiptXml(
        Guid id,
        ISupplyChainRepository repository,
        CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(id, cancellationToken);
        return receipt?.RawXml is not null ? Results.Text(receipt.RawXml, "application/xml") : Results.NotFound();
    }

    private static async Task<IResult> HandleImportXmlReceipt(
        [FromForm] IFormFile file,
        ImportXmlReceipt importXml,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");
        using var reader = new StreamReader(file.OpenReadStream());
        var xmlContent = await reader.ReadToEndAsync(cancellationToken);
        var result = await importXml.ExecuteAsync("system", xmlContent, Guid.NewGuid().ToString(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandlePreviewXmlReceipt(
        [FromForm] IFormFile file,
        INfeProvider nfeProvider,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded.");
        using var reader = new StreamReader(file.OpenReadStream());
        var xmlContent = await reader.ReadToEndAsync(cancellationToken);
        var result = nfeProvider.Parse(xmlContent);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleImportXmlReceiptBatch(
        [FromForm] IFormFileCollection files,
        ImportXmlReceiptBatch importBatch,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0) return Results.BadRequest("No files uploaded.");
        var documents = new List<ImportXmlReceiptBatchDocument>();
        foreach (var file in files)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            documents.Add(new ImportXmlReceiptBatchDocument(file.FileName, await reader.ReadToEndAsync(cancellationToken)));
        }
        
        var result = await importBatch.ExecuteAsync("system", documents, Guid.NewGuid().ToString(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleStartConference(
        Guid id,
        HttpContext context,
        StartMaterialReceiptConference startConference,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await startConference.ExecuteAsync(id, actor, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ToProblemResult("association.invalid_receipt_status", ex.Message);
        }
    }

    private static async Task<IResult> HandleCloseConference(
        Guid id,
        [FromBody] CloseConferenceRequest request,
        HttpContext context,
        CloseMaterialReceiptConference closeConference,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name ?? "system";
        try
        {
            var result = await closeConference.ExecuteAsync(
                id, 
                actor, 
                request.Results.Select(i => new CloseConferenceItemInput(i.ItemId, i.CountedQuantity, i.ConfirmedLotNumber, i.ConfirmedExpirationDate)).ToList(),
                Guid.NewGuid().ToString(),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ToProblemResult("conference.invalid_operation", ex.Message);
        }
    }

    private static async Task<IResult> HandleGetConferenceItems(
        Guid id,
        GetConferenceItems getItems,
        CancellationToken cancellationToken)
    {
        var items = await getItems.ExecuteAsync(id, cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> HandleListOutboxDeadLetters(
        ListSupplyOutboxDeadLetters listDeadLetters,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var result = await listDeadLetters.ExecuteAsync(take, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleReplayOutboxDeadLetters(
        [FromBody] ReplayOutboxDeadLettersRequest request,
        ReplaySupplyOutboxDeadLetters replayDeadLetters,
        CancellationToken cancellationToken)
    {
        await replayDeadLetters.ExecuteAsync(request.MessageIds, cancellationToken);
        return Results.Accepted();
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetSupplyChainInfo getInfo)
    {
        var tenant = context.GetResolvedTenant();
        var response = getInfo.Execute(environment.EnvironmentName, tenant?.Locale, tenant?.TimeZone);
        return Results.Ok(response);
    }

    private static IResult ToProblemResult(string code, string message)
    {
        return Results.Problem(
            title: "Operation failed",
            detail: message,
            statusCode: code switch
            {
                "receipt.not_found" or "association.item_not_found" or "supplier.not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" or "conference.already_started" => StatusCodes.Status409Conflict,
                "concurrency_error" or "association.conflict" => StatusCodes.Status412PreconditionFailed,
                _ => StatusCodes.Status400BadRequest
            },
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private static IResult ToProblemResult(RailFactory.BuildingBlocks.Results.Error error)
    {
        return ToProblemResult(error.Code, error.Message);
    }
}
