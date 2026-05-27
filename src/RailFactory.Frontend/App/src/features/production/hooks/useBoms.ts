import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listBoms } from '../api/production';
import type { Bom } from '../types';

/**
 * Fetches all BOMs (optionally filtered by product code) for a given tenant.
 * Re-fetches automatically when `tenantCode` or `productCode` changes.
 */
export function useBoms(tenantCode: string, productCode?: string): UseQueryResult<Bom[]> {
  return useQuery(
    (signal) => listBoms(tenantCode, productCode, signal),
    [tenantCode, productCode ?? null],
    'Falha ao carregar BOMs'
  );
}
