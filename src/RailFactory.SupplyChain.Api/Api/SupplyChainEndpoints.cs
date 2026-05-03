using Microsoft.AspNetCore.Mvc;
using RailFactory.SupplyChain.Api.Api.Requests;
using RailFactory.SupplyChain.Api.Api.Validation;
using RailFactory.SupplyChain.Api.Application;
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
    private const string ImportXmlPath = "/receipts/import/xml";

    public static WebApplication MapSupplyChainEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapPost(SuppliersPath, HandleCreateSupplier);
        app.MapPost(ReceiptsPath, HandleCreateReceipt);
        app.MapGet(ReceiptsPath, HandleListReceipts);
        app.MapPost(ImportXmlPath, HandleImportXmlReceipt);
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetSupplyChainInfo getSupplyChainInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getSupplyChainInfo.Execute(
            environment.EnvironmentName,
            tenant?.Code,
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

    private static async Task<IResult> HandleCreateReceipt(
        HttpContext context,
        [FromBody] CreateManualReceiptRequest request,
        CreateManualReceipt createManualReceipt,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var correlationId = context.TraceIdentifier;
        var userIdentifier = context.User.Identity?.Name ?? "anonymous";

        MaterialReceipt receipt;
        try
        {
            receipt = await createManualReceipt.ExecuteAsync(
                tenantCode,
                userIdentifier,
                request.ReceiptNumber,
                request.SupplierId,
                request.DocumentNumber,
                request.ReceiptDate,
                request.Items.Select(x => new CreateManualReceiptItemInput(x.MaterialCode, x.ExpectedQuantity, x.UnitOfMeasure)).ToList(),
                correlationId,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "receipt.invalid_state" });
        }

        return Results.Created($"{ReceiptsPath}/{receipt.Id}", new { receipt.Id, receipt.ReceiptNumber, Items = receipt.Items.Count });
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

        var receipts = await listReceipts.ExecuteAsync(tenantCode, cancellationToken);
        var response = receipts.Select(x => new
        {
            x.Id,
            x.ReceiptNumber,
            x.DocumentNumber,
            x.ReceiptDate,
            x.Status,
            x.CreatedAt,
            itemCount = x.Items.Count,
            items = x.Items.Select(i => new
            {
                i.Id,
                i.MaterialCode,
                i.ExpectedQuantity,
                i.UnitOfMeasure
            })
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleImportXmlReceipt(
        HttpContext context,
        [FromBody] ImportXmlReceiptRequest request,
        ImportXmlReceipt importXmlReceipt,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        Guid receiptId;
        try
        {
            receiptId = await importXmlReceipt.ExecuteAsync(
                tenantCode,
                context.User.Identity?.Name ?? "anonymous",
                request.XmlContent,
                context.TraceIdentifier,
                cancellationToken);
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
}
