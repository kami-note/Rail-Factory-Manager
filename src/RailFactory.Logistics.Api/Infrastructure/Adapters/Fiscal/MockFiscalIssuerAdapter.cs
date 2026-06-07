using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

public sealed class MockFiscalIssuerAdapter : IFiscalIssuerAdapter
{
    public string ProviderType => "mock";

    public Task<NfeEmissionResult> EmitirNfeAsync(NfeRequest request, CancellationToken cancellationToken = default)
    {
        var fakeAccessKey = $"35{DateTime.UtcNow:yyyyMMdd}00000000000000055{Random.Shared.Next(100000000, 999999999):D9}00000001";
        return Task.FromResult(new NfeEmissionResult(
            ExternalId: $"mock-{request.RefCode}",
            Status: "autorizado",
            AccessKey: fakeAccessKey,
            AuthorizationProtocol: $"1{Random.Shared.Next(100000000, 999999999):D14}",
            PdfUrl: null,
            XmlUrl: null,
            ErrorMessage: null));
    }

    public Task<NfeStatusResult> ConsultarStatusAsync(string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NfeStatusResult(externalId, "autorizado", $"35{DateTime.UtcNow:yyyyMMdd}mock", null));

    public Task<bool> CancelarAsync(string externalId, string justificativa, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<MdfeEmissionResult> EmitirMdfeAsync(MdfeRequest request, CancellationToken cancellationToken = default)
    {
        var fakeAccessKey = $"35{DateTime.UtcNow:yyyyMMdd}000000000000000{Random.Shared.Next(10000000, 99999999):D8}00001";
        return Task.FromResult(new MdfeEmissionResult(
            ExternalId: $"mock-mdfe-{request.RefCode}",
            Status: "autorizado",
            AccessKey: fakeAccessKey,
            PdfUrl: null,
            ErrorMessage: null));
    }

    public Task<bool> EncerrarMdfeAsync(string externalId, string ufEncerramento, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
