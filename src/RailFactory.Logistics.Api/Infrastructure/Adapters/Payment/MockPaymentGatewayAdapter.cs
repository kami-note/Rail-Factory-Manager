using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

/// <summary>
/// Mock implementation of <see cref="IPaymentGatewayAdapter"/> for development and testing.
/// Returns deterministic fake boleto and PIX URLs based on the external reference.
/// </summary>
/// <remarks>
/// In production, configure Asaas credentials in the tenant integration settings.
/// This mock does NOT perform any real payment operation.
/// </remarks>
internal sealed class MockPaymentGatewayAdapter : IPaymentGatewayAdapter
{
    public string ProviderType => "mock";

    public Task<PaymentChargeResult> CreateChargeAsync(
        PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        var fakeId = $"mock-{Guid.NewGuid():N}"[..20];
        // Generate deterministic mock URLs so the UI can render download buttons
        var boletoUrl = $"https://mock.payment/boleto/{request.ExternalReference}.pdf";
        var pixUrl = $"https://mock.payment/pix/{request.ExternalReference}";
        return Task.FromResult(new PaymentChargeResult(fakeId, "PENDING", boletoUrl, pixUrl, null));
    }

    public Task<PaymentChargeResult> GetStatusAsync(
        string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentChargeResult(externalId, "PENDING", null, null, null));
}
