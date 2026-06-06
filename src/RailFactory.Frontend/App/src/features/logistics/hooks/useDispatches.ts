import { useQuery } from '../../../shared/lib/useQuery';
import { listDispatches } from '../api/logistics';
import type { Dispatch } from '../types';

export function useDispatches(tenantCode: string) {
  return useQuery<Dispatch[]>(
    (signal) => listDispatches(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar despachos'
  );
}
