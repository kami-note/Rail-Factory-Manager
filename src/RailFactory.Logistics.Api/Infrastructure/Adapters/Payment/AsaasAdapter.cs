using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Payment;

public sealed class AsaasAdapter(HttpClient httpClient) : IPaymentGatewayAdapter
{
    public string ProviderType => "asaas";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<PaymentChargeResult> CreateChargeAsync(
        PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var customerId = await EnsureCustomerAsync(request, cancellationToken);

            var chargeBody = new
            {
                customer = customerId,
                billingType = request.BillingType,
                dueDate = request.DueDate.ToString("yyyy-MM-dd"),
                value = request.ValueBrl,
                description = request.Description,
                externalReference = request.ExternalReference
            };

            using var response = await httpClient.PostAsJsonAsync("payments", chargeBody, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                return Error(request.ExternalReference, $"Asaas create payment error {(int)response.StatusCode}: {Truncate(err)}");
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var status = root.TryGetProperty("status", out var stProp) ? stProp.GetString() ?? "PENDING" : "PENDING";
            var boletoUrl = root.TryGetProperty("bankSlipUrl", out var buProp) ? buProp.GetString() : null;
            // invoiceUrl is the generic payment link (works for all billing types)
            var invoiceUrl = root.TryGetProperty("invoiceUrl", out var ivProp) ? ivProp.GetString() : null;

            if (string.IsNullOrEmpty(id))
                return Error(request.ExternalReference, "Asaas payment response missing 'id'.");

            return new PaymentChargeResult(id, status, boletoUrl, invoiceUrl, null);
        }
        catch (Exception ex)
        {
            return Error(request.ExternalReference, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<PaymentChargeResult> GetStatusAsync(
        string externalId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"payments/{Uri.EscapeDataString(externalId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "UNKNOWN" : "UNKNOWN";
        var boletoUrl = root.TryGetProperty("bankSlipUrl", out var bu) ? bu.GetString() : null;
        var invoiceUrl = root.TryGetProperty("invoiceUrl", out var iv) ? iv.GetString() : null;

        return new PaymentChargeResult(externalId, status, boletoUrl, invoiceUrl, null);
    }

    private async Task<string> EnsureCustomerAsync(PaymentChargeRequest request, CancellationToken cancellationToken)
    {
        var cpfCnpj = request.CustomerCpfCnpj.Replace(".", "").Replace("/", "").Replace("-", "");

        using var searchResponse = await httpClient.GetAsync(
            $"customers?cpfCnpj={Uri.EscapeDataString(cpfCnpj)}", cancellationToken);

        if (searchResponse.IsSuccessStatusCode)
        {
            using var searchDoc = await JsonDocument.ParseAsync(
                await searchResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (searchDoc.RootElement.TryGetProperty("data", out var data) &&
                data.GetArrayLength() > 0 &&
                data[0].TryGetProperty("id", out var existingId))
            {
                return existingId.GetString()!;
            }
        }

        var customerBody = new
        {
            name = request.CustomerName,
            cpfCnpj,
            email = string.IsNullOrEmpty(request.CustomerEmail) ? (string?)null : request.CustomerEmail
        };

        using var createResponse = await httpClient.PostAsJsonAsync("customers", customerBody, JsonOptions, cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            var errBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Asaas create customer error {(int)createResponse.StatusCode}: {Truncate(errBody)}");
        }

        using var createDoc = await JsonDocument.ParseAsync(
            await createResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        return createDoc.RootElement.GetProperty("id").GetString()!;
    }

    private static PaymentChargeResult Error(string refCode, string message) =>
        new(refCode, "error", null, null, message);

    private static string Truncate(string s) => s[..Math.Min(500, s.Length)];
}
