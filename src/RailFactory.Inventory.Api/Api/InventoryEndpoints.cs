using Microsoft.AspNetCore.Mvc;
using RailFactory.Inventory.Api.Api.Requests;
using RailFactory.Inventory.Api.Api.Validation;
using RailFactory.Inventory.Api.Application;
using RailFactory.Inventory.Api.Application.Balances;

namespace RailFactory.Inventory.Api.Api;

public static class InventoryEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";
    private const string PendingBalancesPath = "/balances/pending";
    private const string PendingBalanceInternalPath = "/internal/pending-balances";

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapGet(PendingBalancesPath, HandleListPendingBalances);
        app.MapPost(PendingBalanceInternalPath, HandleCreatePendingBalanceInternal);
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetInventoryInfo getInventoryInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getInventoryInfo.Execute(
            environment.EnvironmentName,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleListPendingBalances(
        HttpContext context,
        ListPendingBalances listPendingBalances,
        CancellationToken cancellationToken)
    {
        var tenantCode = context.GetResolvedTenant()?.Code;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var balances = await listPendingBalances.ExecuteAsync(cancellationToken);
        return Results.Ok(balances.Select(x => new
        {
            x.Id,
            x.MaterialCode,
            x.Quantity,
            x.UnitOfMeasure,
            x.Status,
            x.SourceReference,
            x.CreatedAt
        }));
    }

    private static async Task<IResult> HandleCreatePendingBalanceInternal(
        [FromBody] CreatePendingBalanceRequest request,
        CreatePendingBalance createPendingBalance,
        CancellationToken cancellationToken)
    {
        var validation = RequestValidator.Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        bool created;
        try
        {
            created = await createPendingBalance.ExecuteAsync(
                new CreatePendingBalanceInput(
                    request.EventId,
                    request.EventType,
                    request.CorrelationId,
                    request.Payload.ReceiptId,
                    request.Payload.ReceiptItemId,
                    request.Payload.ReceiptNumber,
                    request.Payload.MaterialCode,
                    request.Payload.Quantity,
                    request.Payload.UnitOfMeasure),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Invalid integration request",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = "integration.invalid_payload" });
        }

        return created ? Results.Accepted() : Results.Ok(new { status = "duplicate_ignored" });
    }
}
