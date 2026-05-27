import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { getInventoryDashboard } from '../api/dashboard';
import type { InventoryDashboard } from '../types';

/**
 * Fetches inventory KPI data for the dashboard overview panel.
 *
 * @param tenantCode - The tenant identifier for multi-tenancy resolution.
 * @returns Standard query result with inventory dashboard metrics.
 *
 * @remarks
 * Invariant: Only the Inventory service owns balance data. This hook reads a cached
 * read-model via the dashboard endpoint — it never calls balance endpoints directly.
 */
export function useInventoryDashboard(tenantCode: string): UseQueryResult<InventoryDashboard> {
  return useQuery(
    (signal) => getInventoryDashboard(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar dados de estoque'
  );
}
