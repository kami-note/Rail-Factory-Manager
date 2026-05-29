import { useQuery } from '../../../shared/lib/useQuery';
import { buildTenantHeaders } from '../../../shared/lib/http';
import type { Carrier } from '../types';

export function useCarriers(tenantCode: string) {
  return useQuery<Carrier[]>(
    (signal) =>
      fetch('/api/logistics/carriers', {
        signal,
        credentials: 'include',
        headers: buildTenantHeaders(tenantCode),
      }).then((r) => (r.ok ? r.json() : Promise.reject(new Error(r.statusText)))),
    [tenantCode],
    'Falha ao carregar transportadoras'
  );
}
