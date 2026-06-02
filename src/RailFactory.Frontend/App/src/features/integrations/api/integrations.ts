import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Integration } from '../types';

const BASE = '/api/tenancy/admin/integrations';

export async function listIntegrations(tenantCode: string): Promise<Integration[]> {
  return fetchJsonOrThrow<Integration[]>(
    `${BASE}/`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao carregar integrações'
  );
}

export async function configureIntegration(
  tenantCode: string,
  body: { category: string; providerType: string; credentials: Record<string, string> }
): Promise<{ id: string }> {
  return fetchJsonOrThrow<{ id: string }>(
    `${BASE}/`,
    {
      method: 'POST',
      credentials: 'include',
      headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    },
    'Erro ao configurar integração'
  );
}

export async function enableIntegration(tenantCode: string, category: string): Promise<Integration> {
  return fetchJsonOrThrow<Integration>(
    `${BASE}/${category}/enable`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao habilitar integração'
  );
}

export async function disableIntegration(tenantCode: string, category: string): Promise<Integration> {
  return fetchJsonOrThrow<Integration>(
    `${BASE}/${category}/disable`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao desabilitar integração'
  );
}
