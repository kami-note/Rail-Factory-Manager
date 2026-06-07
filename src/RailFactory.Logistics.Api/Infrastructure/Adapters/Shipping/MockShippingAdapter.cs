using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

public sealed class MockShippingAdapter : IShippingAdapter
{
    public string ProviderType => "mock";

    public Task<ShippingLabelResult> RequestLabelAsync(ShippingLabelRequest request, CancellationToken cancellationToken = default)
    {
        var fakeId = $"MOCK-{request.ReferenceCode}";
        return Task.FromResult(new ShippingLabelResult(
            ExternalId: fakeId,
            Status: "order.generated",
            LabelUrl: $"https://mock.shipping/labels/{fakeId}.pdf",
            TrackingCode: $"BR{Random.Shared.Next(100000000, 999999999)}BR",
            ErrorMessage: null));
    }

    public Task<ShippingLabelResult> GetStatusAsync(string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ShippingLabelResult(externalId, "order.generated", null, null, null));
}
