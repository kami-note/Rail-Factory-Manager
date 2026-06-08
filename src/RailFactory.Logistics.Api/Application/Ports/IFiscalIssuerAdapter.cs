namespace RailFactory.Logistics.Api.Application.Ports;

public sealed record NfeAddress(
    string Street, string Number, string? Complement,
    string District, string City, string State, string ZipCode,
    string CountryCode = "1058", string? CityIbgeCode = null);

public sealed record NfeParty(
    string CnpjOrCpf, string Name, string Email, NfeAddress Address, string? IeStateRegistration = null);

public sealed record NfeItem(
    string Code, string Description, string NcmCode, string CfopCode,
    string UnitOfMeasure, decimal Quantity, decimal UnitValue,
    decimal TaxBaseIcms, decimal IcmsRate, decimal IpiRate = 0m,
    int IcmsOrigin = 0, string IcmsCst = "40", string PisCst = "07", string CofinsCst = "07",
    string IpiCst = "99");

public sealed record NfeRequest(
    string TenantId,
    string RefCode,
    string NatureOfOperation,
    NfeParty Emitter,
    NfeParty Recipient,
    IReadOnlyList<NfeItem> Items,
    string? WebhookCallbackUrl = null,
    int ModalidadeFrete = 0);

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

// ── MDF-e ─────────────────────────────────────────────────────────────────────

public sealed record MdfeVehicle(string Plate, string? Rntrc);

public sealed record MdfeDriver(string CpfCnpj, string Name);

public sealed record MdfeNfeLink(string AccessKey, string Cte = "");

public sealed record MdfeRequest(
    string TenantId,
    string RefCode,
    MdfeVehicle Vehicle,
    MdfeDriver Driver,
    string UfInicio,
    string UfFim,
    IReadOnlyList<string> UfsPercorridas,
    decimal TotalWeightKg,
    decimal TotalValueBrl,
    IReadOnlyList<MdfeNfeLink> NfeLinks,
    string? WebhookCallbackUrl = null);

public sealed record MdfeEmissionResult(
    string ExternalId,
    string Status,
    string? AccessKey,
    string? PdfUrl,
    string? ErrorMessage);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IFiscalIssuerAdapter
{
    string ProviderType { get; }
    Task<NfeEmissionResult> EmitirNfeAsync(NfeRequest request, CancellationToken cancellationToken = default);
    Task<NfeStatusResult> ConsultarStatusAsync(string externalId, CancellationToken cancellationToken = default);
    Task<bool> CancelarAsync(string externalId, string justificativa, CancellationToken cancellationToken = default);
    Task<MdfeEmissionResult> EmitirMdfeAsync(MdfeRequest request, CancellationToken cancellationToken = default);
    Task<bool> EncerrarMdfeAsync(string externalId, string ufEncerramento, CancellationToken cancellationToken = default);
}
