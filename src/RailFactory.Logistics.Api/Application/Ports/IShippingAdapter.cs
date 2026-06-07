namespace RailFactory.Logistics.Api.Application.Ports;

public sealed record ShippingAddress(
    string Name, string Phone, string Document,
    string ZipCode, string Street, string Number,
    string? Complement, string District, string City, string StateAbbr);

public sealed record ShippingPackage(
    decimal WeightKg, decimal HeightCm, decimal WidthCm, decimal LengthCm);

public sealed record ShippingLabelRequest(
    string TenantId,
    string ReferenceCode,
    ShippingAddress From,
    ShippingAddress To,
    IReadOnlyList<ShippingPackage> Packages,
    decimal InsuredValueBrl,
    int ServiceId = 1);

public sealed record ShippingLabelResult(
    string ExternalId,
    string Status,
    string? LabelUrl,
    string? TrackingCode,
    string? ErrorMessage);

public interface IShippingAdapter
{
    string ProviderType { get; }
    Task<ShippingLabelResult> RequestLabelAsync(ShippingLabelRequest request, CancellationToken cancellationToken = default);
    Task<ShippingLabelResult> GetStatusAsync(string externalId, CancellationToken cancellationToken = default);
}
