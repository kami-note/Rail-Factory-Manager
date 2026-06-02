import { useQuery } from '../../../shared/lib/useQuery';
import { buildTenantHeaders } from '../../../shared/lib/http';
import type { Dispatch } from '../types';

export function useDispatches(tenantCode: string) {
  return useQuery<Dispatch[]>(
    (signal) =>
      fetch('/api/logistics/dispatches', {
        signal,
        credentials: 'include',
        headers: buildTenantHeaders(tenantCode),
      }).then((r) => (r.ok ? r.json() : Promise.reject(new Error(r.statusText)))),
    [tenantCode],
    'Falha ao carregar despachos'
  );
}
