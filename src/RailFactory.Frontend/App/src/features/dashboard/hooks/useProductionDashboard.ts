import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { getProductionDashboard } from '../api/dashboard';
import type { ProductionDashboard } from '../types';

/**
 * Fetches production KPI data for the dashboard overview panel.
 *
 * @param tenantCode - The tenant identifier for multi-tenancy resolution.
 * @returns Standard query result with production dashboard metrics.
 *
 * @remarks
 * Invariant: Backend computes all aggregations. The frontend never derives KPIs from raw order data.
 */
export function useProductionDashboard(tenantCode: string): UseQueryResult<ProductionDashboard> {
  return useQuery(
    (signal) => getProductionDashboard(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar dados de produção'
  );
}
