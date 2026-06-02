import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

const BASE = '/api/tenancy/admin/tenants';

export interface TenantSummary {
  code: string;
  displayName: string;
  locale: string;
  timeZone: string;
  status: string;
  connectionStrings: Record<string, string>;
}

export async function listTenants(tenantCode: string): Promise<TenantSummary[]> {
  return fetchJsonOrThrow<TenantSummary[]>(
    `${BASE}/`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao carregar tenants'
  );
}

export async function registerTenant(
  tenantCode: string,
  body: { code: string; displayName: string; locale?: string; timeZone?: string }
): Promise<TenantSummary> {
  return fetchJsonOrThrow<TenantSummary>(
    `${BASE}/`,
    {
      method: 'POST',
      credentials: 'include',
      headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    },
    'Erro ao criar tenant'
  );
}
