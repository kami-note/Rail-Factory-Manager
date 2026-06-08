namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record UpsertFiscalProfileRequest(
    string CfopPadraoIntraestadual,
    string CfopPadraoInterestadual,
    string UfOrigem,
    decimal IcmsRate,
    string IcmsCst,
    string PisCst,
    string CofinsCst,
    decimal IpiRate,
    int IcmsOrigin,
    string EmitterName = "",
    string EmitterCnpj = "",
    string EmitterIe = "",
    string EmitterCity = "",
    string EmitterState = "");
