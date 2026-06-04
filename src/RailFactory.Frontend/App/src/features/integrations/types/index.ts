export type IntegrationCategory = 'fiscal' | 'payment' | 'shipping' | 'telemetry' | 'hr' | 'erp';

export interface Integration {
  id: string;
  tenantId: string;
  category: IntegrationCategory | string;
  providerType: string;
  isEnabled: boolean;
  updatedAt: string;
}

export interface CredentialField {
  key: string;
  label: string;
  placeholder?: string;
  required?: boolean;
  secret?: boolean;
  hint?: string;
}

export interface ProviderSchema {
  providerType: string;
  label: string;
  credentialFields: CredentialField[];
  emitterFields: CredentialField[];
  webhookFields: CredentialField[];
}

export const PROVIDER_SCHEMAS: Record<string, ProviderSchema> = {
  plugnotas: {
    providerType: 'plugnotas',
    label: 'PlugNotas',
    credentialFields: [
      { key: 'api_key', label: 'API Key', required: true, secret: true, placeholder: 'Chave da API PlugNotas' },
      { key: 'base_url', label: 'URL Base (opcional)', placeholder: 'https://api.sandbox.plugnotas.com.br', hint: 'Deixe em branco para usar produção' },
    ],
    emitterFields: [
      { key: 'emitter_cnpj', label: 'CNPJ Emitente', required: true, placeholder: '00.000.000/0000-00' },
      { key: 'emitter_name', label: 'Razão Social', required: true },
      { key: 'emitter_ie', label: 'Inscrição Estadual' },
      { key: 'emitter_email', label: 'E-mail' },
      { key: 'emitter_street', label: 'Logradouro' },
      { key: 'emitter_number', label: 'Número' },
      { key: 'emitter_complement', label: 'Complemento' },
      { key: 'emitter_district', label: 'Bairro' },
      { key: 'emitter_city', label: 'Município' },
      { key: 'emitter_state', label: 'UF', placeholder: 'SP' },
      { key: 'emitter_zip', label: 'CEP' },
    ],
    webhookFields: [
      { key: 'webhook_secret', label: 'Segredo Webhook (opcional)', secret: true, hint: 'Usado para validar chamadas recebidas do PlugNotas' },
    ],
  },
  focusnfe: {
    providerType: 'focusnfe',
    label: 'Focus NFe',
    credentialFields: [
      { key: 'token', label: 'Token de Acesso', required: true, secret: true },
      { key: 'base_url', label: 'URL Base (opcional)', placeholder: 'https://homologacao.focusnfe.com.br', hint: 'Deixe em branco para usar produção' },
    ],
    emitterFields: [
      { key: 'emitter_cnpj', label: 'CNPJ Emitente', required: true, placeholder: '00.000.000/0000-00' },
      { key: 'emitter_name', label: 'Razão Social', required: true },
      { key: 'emitter_ie', label: 'Inscrição Estadual' },
      { key: 'emitter_email', label: 'E-mail' },
      { key: 'emitter_street', label: 'Logradouro' },
      { key: 'emitter_number', label: 'Número' },
      { key: 'emitter_complement', label: 'Complemento' },
      { key: 'emitter_district', label: 'Bairro' },
      { key: 'emitter_city', label: 'Município' },
      { key: 'emitter_state', label: 'UF', placeholder: 'SP' },
      { key: 'emitter_zip', label: 'CEP' },
    ],
    webhookFields: [
      { key: 'webhook_secret', label: 'Segredo Webhook', required: true, secret: true, hint: 'Incluído automaticamente na URL de callback como ?secret=' },
    ],
  },
  mock: {
    providerType: 'mock',
    label: 'Mock (Simulação)',
    credentialFields: [],
    emitterFields: [],
    webhookFields: [],
  },
};

export const CATEGORY_LABELS: Record<string, string> = {
  fiscal: 'Fiscal (NF-e)',
  payment: 'Pagamento',
  shipping: 'Frete',
  telemetry: 'Telemetria',
  hr: 'Ponto / RH',
  erp: 'ERP Backoffice',
};

export const CATEGORY_PROVIDERS: Record<string, string[]> = {
  fiscal: ['plugnotas', 'focusnfe', 'mock'],
  payment: ['asaas', 'iugu', 'mock'],
  shipping: ['melhorenvio', 'intelipost', 'mock'],
  telemetry: ['cobli', 'sascar', 'mock'],
  hr: ['ahgora', 'controlid', 'mock'],
  erp: ['omie', 'sankhya', 'contaazul', 'mock'],
};

export const PROVIDER_METADATA: Record<string, { domain: string, docUrl: string }> = {
  plugnotas: { domain: 'plugnotas.com.br', docUrl: 'https://docs.plugnotas.com.br' },
  focusnfe: { domain: 'focusnfe.com.br', docUrl: 'https://focusnfe.com.br/docs' },
  asaas: { domain: 'asaas.com', docUrl: 'https://docs.asaas.com' },
  iugu: { domain: 'iugu.com', docUrl: 'https://dev.iugu.com' },
  melhorenvio: { domain: 'melhorenvio.com.br', docUrl: 'https://docs.melhorenvio.com.br' },
  intelipost: { domain: 'intelipost.com.br', docUrl: 'https://docs.intelipost.com.br' },
  cobli: { domain: 'cobli.co', docUrl: 'https://docs.cobli.co' },
  sascar: { domain: 'sascar.com.br', docUrl: 'https://www.sascar.com.br' },
  ahgora: { domain: 'ahgora.com.br', docUrl: 'https://docs.ahgora.com.br' },
  controlid: { domain: 'controlid.com.br', docUrl: 'https://www.controlid.com.br' },
  omie: { domain: 'omie.com.br', docUrl: 'https://developer.omie.com.br' },
  sankhya: { domain: 'sankhya.com.br', docUrl: 'https://developer.sankhya.com.br' },
  contaazul: { domain: 'contaazul.com', docUrl: 'https://developers.contaazul.com' },
  mock: { domain: 'example.com', docUrl: '#' }
};
