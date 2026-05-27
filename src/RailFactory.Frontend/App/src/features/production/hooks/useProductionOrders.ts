import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listProductionOrders } from '../api/production';
import type { ProductionOrder } from '../types';

/**
 * Fetches production orders for a given tenant with optional filters.
 * Re-fetches automatically when `tenantCode`, `statusFilter`, or `workCenterFilter` changes.
 */
export function useProductionOrders(
  tenantCode: string,
  statusFilter?: string,
  workCenterFilter?: string
): UseQueryResult<ProductionOrder[]> {
  return useQuery(
    (signal) => listProductionOrders(tenantCode, statusFilter, workCenterFilter, signal),
    [tenantCode, statusFilter ?? null, workCenterFilter ?? null],
    'Falha ao carregar ordens de produção'
  );
}
