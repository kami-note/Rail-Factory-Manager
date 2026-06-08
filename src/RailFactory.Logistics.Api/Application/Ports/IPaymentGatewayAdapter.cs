namespace RailFactory.Logistics.Api.Application.Ports;

public sealed record PaymentChargeRequest(
    string TenantId,
    string ExternalReference,
    string CustomerName,
    string CustomerCpfCnpj,
    string CustomerEmail,
    decimal ValueBrl,
    string Description,
    DateOnly DueDate,
    string BillingType = "BOLETO");

public sealed record PaymentChargeResult(
    string ExternalId,
    string Status,
    string? BoletoUrl,
    string? PixUrl,
    string? ErrorMessage);

public interface IPaymentGatewayAdapter
{
    string ProviderType { get; }
    Task<PaymentChargeResult> CreateChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default);
    Task<PaymentChargeResult> GetStatusAsync(string externalId, CancellationToken cancellationToken = default);
}
