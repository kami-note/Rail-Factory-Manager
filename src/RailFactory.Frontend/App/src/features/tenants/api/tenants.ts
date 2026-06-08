import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

export interface ProvisionStatus {
  tenantCode: string;
  ready: boolean;
  databases: Record<string, 'ready' | 'pending'>;
}

export async function getProvisionStatus(code: string): Promise<ProvisionStatus> {
  const r = await fetch(`/api/tenancy/tenants/${encodeURIComponent(code)}/provision-status`, { credentials: 'include' });
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return r.json() as Promise<ProvisionStatus>;
}

const BASE = '/api/tenancy/admin/tenants';

export interface TenantSummary {
  code: string;
  displayName: string;
  locale: string;
  timeZone: string;
  status: string;
  connectionStrings: Record<string, string>;
}

export interface RegisterTenantInput {
  code: string;
  displayName: string;
  locale: string;
  timeZone: string;
}

export async function listTenants(tenantCode: string): Promise<TenantSummary[]> {
  return fetchJsonOrThrow<TenantSummary[]>(
    BASE,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao listar tenants.'
  );
}

export async function registerTenant(
  tenantCode: string,
  input: RegisterTenantInput
): Promise<TenantSummary> {
  return fetchJsonOrThrow<TenantSummary>(
    BASE,
    {
      method: 'POST',
      credentials: 'include',
      headers: buildTenantHeaders(tenantCode),
      body: JSON.stringify(input),
    },
    'Erro ao criar tenant.'
  );
}

export async function deleteTenant(tenantCode: string, targetCode: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/${targetCode}`,
    { method: 'DELETE', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao remover tenant.'
  );
}
