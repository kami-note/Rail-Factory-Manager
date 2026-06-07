using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Shipping;

public sealed class MelhorEnvioAdapter(HttpClient httpClient) : IShippingAdapter
{
    public string ProviderType => "melhorenvio";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ShippingLabelResult> RequestLabelAsync(
        ShippingLabelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Add to cart → get order UUID
            var cartBody = BuildCartBody(request);
            using var cartResponse = await httpClient.PostAsJsonAsync(
                "/api/v2/me/cart", cartBody, JsonOptions, cancellationToken);

            if (!cartResponse.IsSuccessStatusCode)
            {
                var err = await cartResponse.Content.ReadAsStringAsync(cancellationToken);
                return Error(request.ReferenceCode, $"Cart error {(int)cartResponse.StatusCode}: {Truncate(err)}");
            }

            using var cartDoc = await JsonDocument.ParseAsync(
                await cartResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var orderId = cartDoc.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;
            if (string.IsNullOrEmpty(orderId))
                return Error(request.ReferenceCode, "Melhor Envio cart response missing 'id'.");

            // Step 2: Checkout
            using var checkoutResponse = await httpClient.PostAsJsonAsync(
                "/api/v2/me/shipment/checkout",
                new { orders = new[] { orderId } },
                JsonOptions, cancellationToken);

            if (!checkoutResponse.IsSuccessStatusCode)
            {
                var err = await checkoutResponse.Content.ReadAsStringAsync(cancellationToken);
                return Error(orderId, $"Checkout error {(int)checkoutResponse.StatusCode}: {Truncate(err)}");
            }

            // Step 3: Generate label
            using var generateResponse = await httpClient.PostAsJsonAsync(
                "/api/v2/me/shipment/generate",
                new { orders = new[] { orderId } },
                JsonOptions, cancellationToken);

            if (!generateResponse.IsSuccessStatusCode)
            {
                var err = await generateResponse.Content.ReadAsStringAsync(cancellationToken);
                return Error(orderId, $"Generate error {(int)generateResponse.StatusCode}: {Truncate(err)}");
            }

            // Step 4: Print (get label URL)
            using var printResponse = await httpClient.PostAsJsonAsync(
                "/api/v2/me/shipment/print",
                new { orders = new[] { orderId }, mode = "public" },
                JsonOptions, cancellationToken);

            string? labelUrl = null;
            if (printResponse.IsSuccessStatusCode)
            {
                using var printDoc = await JsonDocument.ParseAsync(
                    await printResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (printDoc.RootElement.TryGetProperty("url", out var urlProp))
                    labelUrl = urlProp.GetString();
            }

            return new ShippingLabelResult(orderId, "order.generated", labelUrl, null, null);
        }
        catch (Exception ex)
        {
            return Error(request.ReferenceCode, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task<ShippingLabelResult> GetStatusAsync(
        string externalId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"/api/v2/me/orders/{Uri.EscapeDataString(externalId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "unknown" : "unknown";
        var tracking = root.TryGetProperty("tracking", out var tr) ? tr.GetString() : null;

        return new ShippingLabelResult(externalId, status, null, tracking, null);
    }

    private static object BuildCartBody(ShippingLabelRequest r) => new
    {
        service = r.ServiceId,
        agency = (int?)null,
        from = new
        {
            name = r.From.Name,
            phone = r.From.Phone,
            email = (string?)null,
            document = r.From.Document,
            address = r.From.Street,
            number = r.From.Number,
            complement = r.From.Complement,
            district = r.From.District,
            city = r.From.City,
            state_abbr = r.From.StateAbbr,
            postal_code = r.From.ZipCode,
            country_id = "BR"
        },
        to = new
        {
            name = r.To.Name,
            phone = r.To.Phone,
            email = (string?)null,
            document = r.To.Document,
            address = r.To.Street,
            number = r.To.Number,
            complement = r.To.Complement,
            district = r.To.District,
            city = r.To.City,
            state_abbr = r.To.StateAbbr,
            postal_code = r.To.ZipCode,
            country_id = "BR"
        },
        products = new[] { new { name = "Mercadorias", quantity = 1, unitary_value = r.InsuredValueBrl } },
        volumes = r.Packages.Select(p => new
        {
            height = (int)Math.Ceiling(p.HeightCm),
            width = (int)Math.Ceiling(p.WidthCm),
            length = (int)Math.Ceiling(p.LengthCm),
            weight = p.WeightKg
        }).ToArray(),
        options = new
        {
            insurance_value = r.InsuredValueBrl,
            receipt = false,
            own_hand = false,
            non_commercial = false,
            tags = new[] { new { tag = r.ReferenceCode, url = (string?)null } }
        }
    };

    private static ShippingLabelResult Error(string refCode, string message) =>
        new(refCode, "error", null, null, message);

    private static string Truncate(string s) => s[..Math.Min(500, s.Length)];
}
