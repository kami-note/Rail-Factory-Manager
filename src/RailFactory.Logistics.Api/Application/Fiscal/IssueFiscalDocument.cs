using RailFactory.BuildingBlocks.Integrations;
using RailFactory.BuildingBlocks.Results;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Application.Fiscal;

public sealed record IssueFiscalDocumentRequest(
    Guid DispatchId,
    string NatureOfOperation,
    NfeParty Emitter,
    NfeParty Recipient,
    IReadOnlyList<NfeItem> Items);

public sealed record FiscalDocumentResult(
    string ExternalId,
    string Status,
    string? AccessKey,
    string? ErrorMessage);

public sealed class IssueFiscalDocument(
    IDispatchRepository dispatches,
    ITenantAdapterFactory<IFiscalIssuerAdapter> adapterFactory,
    ITenantContextAccessor tenantContext)
{
    public async Task<Result<FiscalDocumentResult>> ExecuteAsync(
        IssueFiscalDocumentRequest request, CancellationToken ct = default)
    {
        var dispatch = await dispatches.GetByIdAsync(request.DispatchId, ct);
        if (dispatch is null)
            return Result<FiscalDocumentResult>.Failure(
                Error.NotFound("dispatch.not_found", $"Dispatch {request.DispatchId} not found."));

        var tenantCode = tenantContext.Current?.TenantCode
            ?? throw new InvalidOperationException("Tenant context is not set.");

        var adapter = await adapterFactory.ResolveAsync(tenantCode, ct);

        var nfeRequest = new NfeRequest(
            TenantId: tenantCode,
            RefCode: $"NF-{dispatch.TrackingCode}",
            NatureOfOperation: request.NatureOfOperation,
            Emitter: request.Emitter,
            Recipient: request.Recipient,
            Items: request.Items);

        var result = await adapter.EmitirNfeAsync(nfeRequest, ct);

        dispatch.UpdateFiscalStatus(result.ExternalId, result.Status, result.AccessKey);
        await dispatches.SaveAsync(dispatch, ct);

        return Result<FiscalDocumentResult>.Success(
            new FiscalDocumentResult(result.ExternalId, result.Status, result.AccessKey, result.ErrorMessage));
    }
}
