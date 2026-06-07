import { useQuery } from '../../../shared/lib/useQuery';
import { getFiscalProfile } from '../api/logistics';
import type { TenantFiscalProfile } from '../types';

export function useFiscalProfile(tenantCode: string) {
  return useQuery<TenantFiscalProfile | null>(
    (signal) => getFiscalProfile(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar perfil fiscal'
  );
}
