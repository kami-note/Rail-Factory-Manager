import { useQuery } from '../../../shared/lib/useQuery';
import { buildTenantHeaders } from '../../../shared/lib/http';
import type { Dispatch } from '../types';
import { FILTER_STATUSES } from './fiscalFilters';

export type FiscalPage = { items: Dispatch[]; total: number; totalPages: number };
export const FISCAL_PAGE_SIZE = 30;

export function useFiscalDispatches(tenantCode: string, filter: string, page: number) {
  return useQuery<FiscalPage>(
    (signal) => {
      const statusValues: string[] = FILTER_STATUSES[filter] ?? [];
      const params = new URLSearchParams({ page: String(page), pageSize: String(FISCAL_PAGE_SIZE) });
      statusValues.forEach(s => params.append('status', s));
      return fetch(`/api/logistics/dispatches/fiscal?${params}`, {
        signal,
        credentials: 'include',
        headers: buildTenantHeaders(tenantCode),
      }).then(r => r.ok ? r.json() as Promise<FiscalPage> : Promise.reject(new Error(r.statusText)));
    },
    [tenantCode, filter, page],
    'Falha ao carregar NF-es fiscais.'
  );
}
