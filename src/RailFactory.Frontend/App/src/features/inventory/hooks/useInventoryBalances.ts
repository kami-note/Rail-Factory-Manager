import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listBalances } from '../api/balances';
import type { InventoryBalance } from '../types';

/**
 * Fetches inventory balances for a given tenant, optionally filtered by sourceType.
 * Re-fetches automatically when `tenantCode` or `sourceType` changes.
 */
export function useInventoryBalances(
  tenantCode: string,
  sourceType?: 'Purchase' | 'Production'
): UseQueryResult<InventoryBalance[]> {
  return useQuery(
    (signal) => listBalances(tenantCode, sourceType, signal),
    [tenantCode, sourceType],
    'Falha ao carregar saldos de estoque'
  );
}
