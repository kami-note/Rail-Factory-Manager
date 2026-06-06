import { useQuery } from '../../../shared/lib/useQuery';
import { listCarriers } from '../api/logistics';
import type { Carrier } from '../types';

export function useCarriers(tenantCode: string) {
  return useQuery<Carrier[]>(
    (signal) => listCarriers(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar transportadoras'
  );
}
