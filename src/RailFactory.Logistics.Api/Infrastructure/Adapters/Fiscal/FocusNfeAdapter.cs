using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Infrastructure.Adapters.Fiscal;

public sealed class FocusNfeAdapter(HttpClient httpClient) : IFiscalIssuerAdapter
{
    public string ProviderType => "focusnfe";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<NfeEmissionResult> EmitirNfeAsync(NfeRequest request, CancellationToken cancellationToken = default)
    {
        // FocusNFe expects FLAT snake_case fields at root level (not nested)
        var isConsumidorFinal = request.Recipient.CnpjOrCpf.Length <= 11 ? 1 : 0;
        var localDestino = string.Equals(request.Emitter.Address.State, request.Recipient.Address.State,
            StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        var indicadorIe = string.IsNullOrEmpty(request.Recipient.IeStateRegistration) ? 9 : 1;

        var body = new
        {
            natureza_operacao = request.NatureOfOperation,
            data_emissao = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            tipo_documento = 1,
            finalidade_emissao = 1,
            consumidor_final = isConsumidorFinal,
            presenca_comprador = 9,
            modalidade_frete = request.ModalidadeFrete,
            local_destino = localDestino,
            cnpj_emitente = request.Emitter.CnpjOrCpf,
            inscricao_estadual_emitente = request.Emitter.IeStateRegistration,
            nome_destinatario = request.Recipient.Name,
            cnpj_destinatario = request.Recipient.CnpjOrCpf.Length > 11 ? request.Recipient.CnpjOrCpf : (string?)null,
            cpf_destinatario = request.Recipient.CnpjOrCpf.Length <= 11 ? request.Recipient.CnpjOrCpf : (string?)null,
            inscricao_estadual_destinatario = request.Recipient.IeStateRegistration,
            email_destinatario = request.Recipient.Email,
            logradouro_destinatario = request.Recipient.Address.Street,
            numero_destinatario = request.Recipient.Address.Number,
            complemento_destinatario = request.Recipient.Address.Complement,
            bairro_destinatario = request.Recipient.Address.District,
            municipio_destinatario = request.Recipient.Address.City,
            uf_destinatario = request.Recipient.Address.State,
            cep_destinatario = request.Recipient.Address.ZipCode,
            indicador_inscricao_estadual_destinatario = indicadorIe,
            url_notificacao = request.WebhookCallbackUrl,
            items = request.Items.Select((item, i) => new
            {
                numero_item = i + 1,
                codigo_produto = item.Code,
                descricao = item.Description,
                codigo_ncm = item.NcmCode,
                cfop = item.CfopCode,
                unidade_comercial = item.UnitOfMeasure,
                quantidade_comercial = item.Quantity,
                valor_unitario_comercial = item.UnitValue,
                icms_origem = item.IcmsOrigin,
                icms_situacao_tributaria = item.IcmsCst,
                icms_base_calculo = item.TaxBaseIcms,
                icms_aliquota = item.IcmsRate,
                pis_situacao_tributaria = item.PisCst,
                cofins_situacao_tributaria = item.CofinsCst,
                ipi_situacao_tributaria = item.IpiRate > 0 ? item.IpiCst : null,
                ipi_aliquota = item.IpiRate > 0 ? (decimal?)item.IpiRate : null,
                ipi_base_calculo = item.IpiRate > 0 ? (decimal?)(item.UnitValue * item.Quantity) : null
            }).ToArray()
        };

        // FocusNFe uses ?ref= as idempotency key
        using var response = await httpClient.PostAsJsonAsync(
            $"/v2/nfe?ref={Uri.EscapeDataString(request.RefCode)}", body, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new NfeEmissionResult(
                request.RefCode, "rejected", null, null, null, null,
                $"FocusNFe {(int)response.StatusCode}: {errorBody[..Math.Min(500, errorBody.Length)]}");
        }

        // 202 Accepted — status is "processando_autorizacao"
        return new NfeEmissionResult(request.RefCode, "processando_autorizacao", null, null, null, null, null);
    }

    public async Task<NfeStatusResult> ConsultarStatusAsync(string externalId, CancellationToken cancellationToken = default)
    {
        // Docs: ?completa=1 (not completo)
        using var response = await httpClient.GetAsync(
            $"/v2/nfe/{Uri.EscapeDataString(externalId)}?completa=1", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var root = doc.RootElement;
        var status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "desconhecido" : "desconhecido";
        var accessKey = root.TryGetProperty("chave_nfe", out var ck) ? ck.GetString() : null;
        var error = root.TryGetProperty("mensagem_sefaz", out var msg) ? msg.GetString() : null;

        return new NfeStatusResult(externalId, status, accessKey, error);
    }

    public async Task<bool> CancelarAsync(string externalId, string justificativa, CancellationToken cancellationToken = default)
    {
        // FocusNFe: DELETE /v2/nfe/{ref} — HttpClient.DeleteAsync has no body overload
        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"/v2/nfe/{Uri.EscapeDataString(externalId)}");
        request.Content = JsonContent.Create(new { justificativa }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<MdfeEmissionResult> EmitirMdfeAsync(MdfeRequest request, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            data_emissao = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            uf_carregamento = request.UfInicio,
            uf_descarregamento = request.UfFim,
            cnpj_emitente = request.TenantId,
            rntrc = request.Vehicle.Rntrc,
            placa_veiculo = request.Vehicle.Plate,
            cpf_motorista = request.Driver.CpfCnpj,
            nome_motorista = request.Driver.Name,
            peso_total = request.TotalWeightKg,
            valor_total_carga = request.TotalValueBrl,
            percurso = request.UfsPercorridas.ToArray(),
            nfes = request.NfeLinks.Select(n => new { chave_nfe = n.AccessKey }).ToArray(),
            url_notificacao = request.WebhookCallbackUrl
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"/v2/mdfe?ref={Uri.EscapeDataString(request.RefCode)}", body, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return new MdfeEmissionResult(
                request.RefCode, "rejected", null, null,
                $"FocusNFe MDF-e {(int)response.StatusCode}: {errorBody[..Math.Min(500, errorBody.Length)]}");
        }

        return new MdfeEmissionResult(request.RefCode, "processando_autorizacao", null, null, null);
    }

    public async Task<bool> EncerrarMdfeAsync(string externalId, string ufEncerramento, CancellationToken cancellationToken = default)
    {
        // FocusNFe: DELETE /v2/mdfe/{ref} = encerramento
        using var req = new HttpRequestMessage(HttpMethod.Delete,
            $"/v2/mdfe/{Uri.EscapeDataString(externalId)}");
        req.Content = JsonContent.Create(new { uf_encerramento = ufEncerramento }, options: JsonOptions);
        using var response = await httpClient.SendAsync(req, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
