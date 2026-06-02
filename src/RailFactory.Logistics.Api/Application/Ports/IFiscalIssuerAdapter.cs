namespace RailFactory.Logistics.Api.Application.Ports;

public sealed record NfeAddress(
    string Street, string Number, string? Complement,
    string District, string City, string State, string ZipCode, string CountryCode = "1058");

public sealed record NfeParty(
    string CnpjOrCpf, string Name, string Email, NfeAddress Address, string? IeStateRegistration = null);

public sealed record NfeItem(
    string Code, string Description, string NcmCode, string CfopCode,
    string UnitOfMeasure, decimal Quantity, decimal UnitValue,
    decimal TaxBaseIcms, decimal IcmsRate, decimal IpiRate = 0m);

public sealed record NfeRequest(
    string TenantId,
    string RefCode,
    string NatureOfOperation,
    NfeParty Emitter,
    NfeParty Recipient,
    IReadOnlyList<NfeItem> Items,
    string? WebhookCallbackUrl = null);

public sealed record NfeEmissionResult(
    string ExternalId,
    string Status,
    string? AccessKey,
    string? AuthorizationProtocol,
    string? PdfUrl,
    string? XmlUrl,
    string? ErrorMessage);

public sealed record NfeStatusResult(
    string ExternalId,
    string Status,
    string? AccessKey,
    string? ErrorMessage);

public interface IFiscalIssuerAdapter
{
    string ProviderType { get; }
    Task<NfeEmissionResult> EmitirNfeAsync(NfeRequest request, CancellationToken cancellationToken = default);
    Task<NfeStatusResult> ConsultarStatusAsync(string externalId, CancellationToken cancellationToken = default);
    Task<bool> CancelarAsync(string externalId, string justificativa, CancellationToken cancellationToken = default);
}
