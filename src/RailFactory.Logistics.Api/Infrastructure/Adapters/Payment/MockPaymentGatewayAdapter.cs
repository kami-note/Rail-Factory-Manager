using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

internal sealed class MockPaymentGatewayAdapter : IPaymentGatewayAdapter
{
    public string ProviderType => "mock";

    public Task<PaymentChargeResult> CreateChargeAsync(
        PaymentChargeRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentChargeResult(
            $"mock-{Guid.NewGuid():N}"[..20],
            "PENDING",
            null, null, null));

    public Task<PaymentChargeResult> GetStatusAsync(
        string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentChargeResult(externalId, "PENDING", null, null, null));
}
