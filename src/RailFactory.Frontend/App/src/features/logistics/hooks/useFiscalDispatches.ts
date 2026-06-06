import { useQuery } from '../../../shared/lib/useQuery';
import { fetchJsonOrThrow, buildTenantHeaders } from '../../../shared/lib/http';
import type { Dispatch } from '../types';
import { FILTER_STATUSES } from './fiscalFilters';

export type FiscalPage = { items: Dispatch[]; total: number; totalPages: number };
export const FISCAL_PAGE_SIZE = 30;

export function useFiscalDispatches(tenantCode: string, filter: string, page: number) {
  return useQuery<FiscalPage>(
    (signal) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(FISCAL_PAGE_SIZE) });
      (FILTER_STATUSES[filter] ?? []).forEach(s => params.append('status', s));
      return fetchJsonOrThrow<FiscalPage>(
        `/api/logistics/dispatches/fiscal?${params}`,
        { credentials: 'include', headers: buildTenantHeaders(tenantCode), signal },
        'Falha ao carregar NF-es fiscais.'
      );
    },
    [tenantCode, filter, page],
    'Falha ao carregar NF-es fiscais.'
  );
}
