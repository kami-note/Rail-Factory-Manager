using Microsoft.AspNetCore.Mvc;
using RailFactory.SupplyChain.Api.Api.Requests;
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
    private const string ReceiptDetailsPath = "/receipts/{id:guid}";
    private const string ImportXmlPath = "/receipts/import/xml";
    private const string ImportXmlPreviewPath = "/receipts/import/xml/preview";
    private const string ImportXmlBatchPath = "/receipts/import/xml/batch";
    private const string ReceiptXmlPath = "/receipts/{id:guid}/xml";
    private const string StartConferencePath = "/receipts/{id:guid}/conference/start";
    private const string CloseConferencePath = "/receipts/{id:guid}/conference/close";
    private const string ConferenceItemsPath = "/receipts/{id:guid}/conference/items";
    private const string OutboxDeadLettersPath = "/outbox/dead-letters";

    public static WebApplication MapSupplyChainEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapPost(SuppliersPath, HandleCreateSupplier);
        app.MapGet(ReceiptsPath, HandleListReceipts);
        app.MapGet(ReceiptDetailsPath, HandleGetReceiptDetails);
        app.MapGet(ReceiptXmlPath, HandleGetReceiptXml);
        app.MapPost(StartConferencePath, HandleStartConference);
        app.MapPost(CloseConferencePath, HandleCloseConference);
        app.MapGet(ConferenceItemsPath, HandleGetConferenceItems);
        app.MapPost(ImportXmlPath, HandleImportXmlReceipt).DisableAntiforgery();
        app.MapPost(ImportXmlPreviewPath, HandlePreviewXmlReceipt).DisableAntiforgery();
        app.MapPost(ImportXmlBatchPath, HandleImportXmlReceiptBatch).DisableAntiforgery();
        app.MapGet(OutboxDeadLettersPath, HandleListOutboxDeadLetters);
        return app;
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
        var response = receipts.Select(x => new
        {
            x.Id,
            x.ReceiptNumber,
            x.DocumentNumber,
            x.AccessKey,
            x.TotalValue,
            x.ReceiptDate,
            x.Status,
            x.CreatedAt,
            itemCount = x.Items.Count
        });

        return Results.Ok(response);
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

        // Blind conference: DO NOT return ExpectedQuantity
        var response = receipt.Items.Select(i => new
        {
            i.Id,
            i.MaterialCode,
            i.UnitOfMeasure,
            i.OriginalDescription
        });

        return Results.Ok(response);
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

            var imported = await importXmlReceiptBatch.ExecuteAsync(
                context.User.Identity?.Name ?? "anonymous",
                documents,
                context.TraceIdentifier,
                cancellationToken);

            return Results.Created(ImportXmlBatchPath, new
            {
                imported = imported.Select(x => new
                {
                    x.FileName,
                    x.ReceiptId,
                    x.ReceiptNumber,
                    x.DocumentNumber
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

public sealed record CloseConferenceRequest(IReadOnlyCollection<CountedResultRequest> Results);
public sealed record CountedResultRequest(Guid ItemId, decimal CountedQuantity, string? ConfirmedLotNumber, DateTimeOffset? ConfirmedExpirationDate);
