using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

public sealed class PlugNotasAdapter(HttpClient httpClient) : IFiscalIssuerAdapter
{
    public string ProviderType => "plugnotas";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<NfeEmissionResult> EmitirNfeAsync(NfeRequest request, CancellationToken cancellationToken = default)
    {
        // PlugNotas expects an ARRAY of notes
        var body = new[]
        {
            new
            {
                idIntegracao = request.RefCode,
                naturezaOperacao = request.NatureOfOperation,
                finalidadeEmissao = 1,
                consumidorFinal = true,
                emitente = new
                {
                    cpfCnpj = request.Emitter.CnpjOrCpf,
                    razaoSocial = request.Emitter.Name,
                    inscricaoEstadual = request.Emitter.IeStateRegistration,
                    email = request.Emitter.Email,
                    endereco = new
                    {
                        logradouro = request.Emitter.Address.Street,
                        numero = request.Emitter.Address.Number,
                        complemento = request.Emitter.Address.Complement,
                        bairro = request.Emitter.Address.District,
                        municipio = request.Emitter.Address.City,
                        codigoCidade = request.Emitter.Address.CityIbgeCode,
                        uf = request.Emitter.Address.State,
                        cep = request.Emitter.Address.ZipCode,
                        codigoPais = request.Emitter.Address.CountryCode,
                        pais = "Brasil"
                    }
                },
                destinatario = new
                {
                    cpfCnpj = request.Recipient.CnpjOrCpf,
                    nome = request.Recipient.Name,
                    email = request.Recipient.Email,
                    endereco = new
                    {
                        logradouro = request.Recipient.Address.Street,
                        numero = request.Recipient.Address.Number,
                        complemento = request.Recipient.Address.Complement,
                        bairro = request.Recipient.Address.District,
                        municipio = request.Recipient.Address.City,
                        codigoCidade = request.Recipient.Address.CityIbgeCode,
                        uf = request.Recipient.Address.State,
                        cep = request.Recipient.Address.ZipCode,
                        codigoPais = request.Recipient.Address.CountryCode,
                        pais = "Brasil"
                    }
                },
                produtos = request.Items.Select((item, i) => new
                {
                    codigo = item.Code,
                    descricao = item.Description,
                    ncm = item.NcmCode,
                    cfop = item.CfopCode,
                    unidade = item.UnitOfMeasure,
                    quantidade = item.Quantity,
                    valorUnitario = item.UnitValue,
                    icms = new
                    {
                        origem = item.IcmsOrigin,
                        cst = item.IcmsCst,
                        baseCalculo = item.TaxBaseIcms,
                        aliquota = item.IcmsRate
                    },
                    pis = new { cst = item.PisCst },
                    cofins = new { cst = item.CofinsCst },
                    ipi = item.IpiRate > 0 ? new { cst = "50", aliquota = item.IpiRate } : null
                }).ToArray()
            }
        };

        using var response = await httpClient.PostAsJsonAsync("/nfe", body, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new NfeEmissionResult(
                request.RefCode, "rejected", null, null, null, null,
                $"PlugNotas {(int)response.StatusCode}: {errorBody[..Math.Min(500, errorBody.Length)]}");
        }

        // 202 Accepted — async emission; returns array of results
        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var first = doc.RootElement.ValueKind == JsonValueKind.Array
            ? doc.RootElement[0]
            : doc.RootElement;

        var id = first.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? request.RefCode : request.RefCode;
        var status = first.TryGetProperty("status", out var stProp) ? stProp.GetString() ?? "processando" : "processando";

        return new NfeEmissionResult(id, status, null, null, null, null, null);
    }

    public async Task<NfeStatusResult> ConsultarStatusAsync(string externalId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/nfe/{Uri.EscapeDataString(externalId)}", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "unknown" : "unknown";
        // Docs: access key field is "chave"
        var accessKey = root.TryGetProperty("chave", out var ck) ? ck.GetString() : null;

        return new NfeStatusResult(externalId, status, accessKey, null);
    }

    public async Task<bool> CancelarAsync(string externalId, string justificativa, CancellationToken cancellationToken = default)
    {
        // PlugNotas: POST /nfe/{id}/cancelamento (not DELETE)
        var body = new { justificativa };
        using var response = await httpClient.PostAsJsonAsync(
            $"/nfe/{Uri.EscapeDataString(externalId)}/cancelamento", body, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
