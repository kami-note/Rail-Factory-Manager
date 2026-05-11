using Microsoft.AspNetCore.Mvc;
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
    private const string RootPath = "/";
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
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapPost(SuppliersPath, HandleCreateSupplier);
        app.MapGet(ReceiptsPath, HandleListReceipts);
        app.MapGet(AssociationQueuePath, HandleListAssociationQueue);
        app.MapGet(ReceiptDetailsPath, HandleGetReceiptDetails);
        app.MapGet(AssociationWorkbenchPath, HandleGetAssociationWorkbench);
        app.MapGet(ReceiptItemsToAssociatePath, HandleGetItemsToAssociate);
        app.MapPost(AssociateReceiptItemPath, HandleAssociateReceiptItem);
        app.MapPost(CreateMaterialAndAssociatePath, HandleCreateMaterialAndAssociate);
        app.MapPost(ReviewAssociationItemLaterPath, HandleReviewAssociationItemLater);
        app.MapPost(IgnoreAssociationItemPath, HandleIgnoreAssociationItem);
        app.MapPost(OverrideSupplierSkuPath, HandleOverrideSupplierSku);
        app.MapPost(ReleaseToConferencePath, HandleReleaseToConference);
        app.MapPost(MappingsPath, HandleCreateMapping);
        app.MapGet(ReceiptXmlPath, HandleGetReceiptXml);
        app.MapPost(StartConferencePath, HandleStartConference);
        app.MapPost(CloseConferencePath, HandleCloseConference);
        app.MapGet(ConferenceItemsPath, HandleGetConferenceItems);
        app.MapPost(ImportXmlPath, HandleImportXmlReceipt).DisableAntiforgery();
        app.MapPost(ImportXmlPreviewPath, HandlePreviewXmlReceipt).DisableAntiforgery();
        app.MapPost(ImportXmlBatchPath, HandleImportXmlReceiptBatch).DisableAntiforgery();
        app.MapGet(OutboxDeadLettersPath, HandleListOutboxDeadLetters);
        app.MapPost(ReplayOutboxDeadLettersPath, HandleReplayOutboxDeadLetters);
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

    private static async Task<IResult> HandleListAssociationQueue(
        HttpContext context,
        ListAssociationQueue listAssociationQueue,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var result = await listAssociationQueue.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetAssociationWorkbench(
        Guid id,
        HttpContext context,
        GetAssociationWorkbench getAssociationWorkbench,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var result = await getAssociationWorkbench.ExecuteAsync(id, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleAssociateReceiptItem(
        Guid receiptId,
        Guid itemId,
        [FromBody] AssociateReceiptItemRequest request,
        HttpContext context,
        AssociateReceiptItem associateReceiptItem,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var result = await associateReceiptItem.ExecuteAsync(
                receiptId,
                itemId,
                request.ExpectedVersion,
                request.InternalMaterialCode,
                request.ConversionFactor,
                context.User.Identity?.Name ?? "system@railfactory.local",
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationConflictException ex)
        {
            return Results.Problem(
                title: "Association item was modified",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.item_conflict", ["itemId"] = ex.ItemId, ["currentVersion"] = ex.CurrentVersion });
        }
        catch (AssociationValidationException ex)
        {
            var statusCode = ex.Code switch
            {
                "association.material_not_found" or "receipt.not_found" or "supplier.not_found" or "association.item_not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: "Invalid association request",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static async Task<IResult> HandleCreateMaterialAndAssociate(
        Guid receiptId,
        Guid itemId,
        [FromBody] CreateMaterialAndAssociateRequest request,
        HttpContext context,
        CreateMaterialAndAssociate createMaterialAndAssociate,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var materialInput = new CreateMaterialInput(
                request.MaterialCode,
                request.OfficialName,
                request.Description,
                request.UnitOfMeasure,
                request.ProcurementType,
                request.Category,
                request.Gtin,
                request.Ncm);

            var result = await createMaterialAndAssociate.ExecuteAsync(
                receiptId,
                itemId,
                request.ExpectedVersion,
                materialInput,
                request.ConversionFactor,
                context.User.Identity?.Name ?? "system@railfactory.local",
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationConflictException ex)
        {
            return Results.Problem(
                title: "Association item was modified",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.item_conflict", ["itemId"] = ex.ItemId, ["currentVersion"] = ex.CurrentVersion });
        }
        catch (AssociationValidationException ex)
        {
            var statusCode = ex.Code switch
            {
                "receipt.not_found" or "supplier.not_found" or "association.item_not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: "Invalid association and creation request",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static Task<IResult> HandleReviewAssociationItemLater(
        Guid receiptId,
        Guid itemId,
        [FromBody] ControlledAssociationDecisionRequest request,
        HttpContext context,
        RecordControlledAssociationDecision recordDecision,
        CancellationToken cancellationToken)
        => HandleControlledAssociationDecision(
            receiptId,
            itemId,
            request,
            context,
            recordDecision,
            ControlledAssociationDecision.ReviewLater,
            cancellationToken);

    private static Task<IResult> HandleIgnoreAssociationItem(
        Guid receiptId,
        Guid itemId,
        [FromBody] ControlledAssociationDecisionRequest request,
        HttpContext context,
        RecordControlledAssociationDecision recordDecision,
        CancellationToken cancellationToken)
        => HandleControlledAssociationDecision(
            receiptId,
            itemId,
            request,
            context,
            recordDecision,
            ControlledAssociationDecision.Ignored,
            cancellationToken);

    private static async Task<IResult> HandleControlledAssociationDecision(
        Guid receiptId,
        Guid itemId,
        ControlledAssociationDecisionRequest request,
        HttpContext context,
        RecordControlledAssociationDecision recordDecision,
        ControlledAssociationDecision decision,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var result = await recordDecision.ExecuteAsync(
                receiptId,
                itemId,
                request.ExpectedVersion,
                decision,
                request.Reason,
                context.User.Identity?.Name ?? "system@railfactory.local",
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationConflictException ex)
        {
            return Results.Problem(
                title: "Association item was modified",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.item_conflict", ["itemId"] = ex.ItemId, ["currentVersion"] = ex.CurrentVersion });
        }
        catch (AssociationValidationException ex)
        {
            var statusCode = ex.Code switch
            {
                "receipt.not_found" or "association.item_not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: "Invalid association decision",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
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
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var actor = context.User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(actor)) return Results.Unauthorized();

            var result = await overrideSku.ExecuteAsync(
                receiptId,
                itemId,
                request.ExpectedVersion,
                request.CorrectedCode,
                request.Reason,
                actor,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationConflictException ex)
        {
            return Results.Problem(
                title: "Association item was modified",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.item_conflict", ["itemId"] = ex.ItemId, ["currentVersion"] = ex.CurrentVersion });
        }
        catch (AssociationValidationException ex)
        {
            var statusCode = ex.Code switch
            {
                "receipt.not_found" or "association.item_not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: "Invalid SKU override request",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static async Task<IResult> HandleReleaseToConference(
        Guid receiptId,
        [FromBody] ReleaseReceiptToConferenceRequest request,
        HttpContext context,
        ReleaseReceiptToConference releaseReceiptToConference,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var result = await releaseReceiptToConference.ExecuteAsync(
                receiptId,
                request.ExpectedReceiptVersion,
                context.User.Identity?.Name ?? "system@railfactory.local",
                cancellationToken);

            return Results.Ok(result);
        }
        catch (AssociationConflictException ex)
        {
            return Results.Problem(
                title: "Association receipt was modified",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.item_conflict", ["receiptId"] = ex.ItemId, ["currentVersion"] = ex.CurrentVersion });
        }
        catch (AssociationReleaseBlockedException ex)
        {
            return Results.Problem(
                title: "Receipt has unresolved association items",
                detail: ex.Message,
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = "association.release_blocked", ["blockers"] = ex.Blockers });
        }
        catch (AssociationValidationException ex)
        {
            var statusCode = ex.Code switch
            {
                "receipt.not_found" => StatusCodes.Status404NotFound,
                "association.invalid_receipt_status" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            return Results.Problem(
                title: "Invalid association release request",
                detail: ex.Message,
                statusCode: statusCode,
                extensions: new Dictionary<string, object?> { ["code"] = ex.Code });
        }
    }

    private static async Task<IResult> HandleCreateMapping(
        [FromBody] CreateSupplierMaterialMappingRequest request,
        HttpContext context,
        CreateSupplierMaterialMapping createMapping,
        CancellationToken cancellationToken)
    {
        var actor = context.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(actor))
        {
            return Results.Unauthorized();
        }

        await createMapping.ExecuteAsync(
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

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetSupplyChainInfo getSupplyChainInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getSupplyChainInfo.Execute(
            environment.EnvironmentName,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleCreateSupplier(
        [FromBody] CreateSupplierRequest request,
        CreateSupplier createSupplier,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var supplier = await createSupplier.ExecuteAsync(request.FiscalId, request.Name, cancellationToken);
        return Results.Created($"{SuppliersPath}/{supplier.Id}", new { supplier.Id, supplier.FiscalId, supplier.Name });
    }

    private static async Task<IResult> HandleListReceipts(
        HttpContext context,
        ListReceipts listReceipts,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var receipts = await listReceipts.ExecuteAsync(cancellationToken);
        return Results.Ok(receipts);
    }

    private static async Task<IResult> HandleGetReceiptDetails(
        Guid id,
        HttpContext context,
        GetMaterialReceiptDetails getDetails,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var result = await getDetails.ExecuteAsync(id, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandlePreviewXmlReceipt(
        HttpContext context,
        IFormFile file,
        INfeProvider nfeProvider,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "receipt.file_required", message = "An XML file is required." });
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var xmlContent = await reader.ReadToEndAsync(cancellationToken);
            var parsed = nfeProvider.Parse(xmlContent);

            return Results.Ok(parsed);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return Results.Problem(
                title: "Invalid fiscal document",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.invalid_xml" });
        }
    }

    private static async Task<IResult> HandleStartConference(
        Guid id,
        HttpContext context,
        StartMaterialReceiptConference startConference,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var receipt = await startConference.ExecuteAsync(
                id,
                context.User.Identity?.Name ?? "anonymous",
                cancellationToken);

            return Results.Ok(new { receiptId = receipt.Id, status = receipt.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.invalid_status" });
        }
    }

    private static async Task<IResult> HandleGetConferenceItems(
        Guid id,
        HttpContext context,
        GetConferenceItems getConferenceItems,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var result = await getConferenceItems.ExecuteAsync(id, cancellationToken);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> HandleCloseConference(
        Guid id,
        [FromBody] CloseConferenceRequest request,
        HttpContext context,
        CloseMaterialReceiptConference closeConference,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var results = request.Results.Select(x => new CloseConferenceItemInput(
                x.ItemId,
                x.CountedQuantity,
                x.ConfirmedLotNumber,
                x.ConfirmedExpirationDate)).ToList();

            var receipt = await closeConference.ExecuteAsync(
                id,
                context.User.Identity?.Name ?? "anonymous",
                results,
                context.TraceIdentifier,
                cancellationToken);

            return Results.Ok(new { receiptId = receipt.Id, status = receipt.Status.ToString() });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.invalid_status" });
        }
    }

    private static async Task<IResult> HandleImportXmlReceipt(
        HttpContext context,
        IFormFile file,
        ImportXmlReceipt importXmlReceipt,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "receipt.file_required", message = "An XML file is required." });
        }

        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        Guid receiptId;
        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var xmlContent = await reader.ReadToEndAsync(cancellationToken);

            receiptId = await importXmlReceipt.ExecuteAsync(
                context.User.Identity?.Name ?? "anonymous",
                xmlContent,
                context.TraceIdentifier,
                cancellationToken);
        }
        catch (ReceiptAlreadyExistsException ex)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.duplicate" });
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.invalid_xml" });
        }

        return Results.Created($"{ReceiptsPath}/{receiptId}", new { receiptId });
    }

    private static async Task<IResult> HandleImportXmlReceiptBatch(
        HttpContext context,
        IFormFileCollection files,
        ImportXmlReceiptBatch importXmlReceiptBatch,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return Results.BadRequest(new { code = "receipt.files_required", message = "At least one XML file is required." });
        }

        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        try
        {
            var documents = new List<ImportXmlReceiptBatchDocument>();
            foreach (var file in files)
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var xmlContent = await reader.ReadToEndAsync(cancellationToken);
                documents.Add(new ImportXmlReceiptBatchDocument(file.FileName, xmlContent));
            }

            var summary = await importXmlReceiptBatch.ExecuteAsync(
                context.User.Identity?.Name ?? "anonymous",
                documents,
                context.TraceIdentifier,
                cancellationToken);

            return Results.Created(ImportXmlBatchPath, new
            {
                successful = summary.SuccessfulImports.Select(x => new
                {
                    x.FileName,
                    x.ReceiptId,
                    x.ReceiptNumber,
                    x.DocumentNumber
                }),
                failed = summary.FailedImports.Select(x => new
                {
                    x.FileName,
                    x.Message
                })
            });
        }
        catch (ImportXmlReceiptBatchValidationException ex)
        {
            return Results.BadRequest(new
            {
                code = "receipt.batch_invalid",
                errors = ex.Errors.Select(x => new { x.FileName, x.Message })
            });
        }
    }

    private static async Task<IResult> HandleListOutboxDeadLetters(
        HttpContext context,
        [FromQuery] int take,
        ListSupplyOutboxDeadLetters listDeadLetters,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var messages = await listDeadLetters.ExecuteAsync(take == 0 ? 50 : take, cancellationToken);
        return Results.Ok(new
        {
            deadLetters = messages.Select(x => new
            {
                x.Id,
                x.EventType,
                x.CorrelationId,
                x.CreatedAt,
                x.AttemptCount,
                x.LastAttemptAt,
                x.DeadLetteredAt,
                x.LastError
            })
        });
    }

    private static async Task<IResult> HandleReplayOutboxDeadLetters(
        HttpContext context,
        [FromBody] ReplayOutboxDeadLettersRequest request,
        ReplaySupplyOutboxDeadLetters replayDeadLetters,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var count = await replayDeadLetters.ExecuteAsync(request.MessageIds, cancellationToken);
        return Results.Ok(new { replayedCount = count });
    }

    private static async Task<IResult> HandleGetReceiptXml(
        Guid id,
        HttpContext context,
        ISupplyChainRepository repository,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var receipt = await repository.GetReceiptByIdAsync(id, cancellationToken);
        if (receipt is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(receipt.RawXml))
        {
            return Results.NotFound(new { code = "receipt.xml_not_found", message = "Raw XML not available for this receipt." });
        }

        return Results.Content(receipt.RawXml, "application/xml");
    }
}
