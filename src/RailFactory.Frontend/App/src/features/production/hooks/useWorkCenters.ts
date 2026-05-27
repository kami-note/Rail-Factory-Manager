import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listWorkCenters } from '../api/production';
import type { WorkCenter } from '../types';

/**
 * Fetches the list of work centers for a given tenant.
 * Re-fetches automatically when `tenantCode` changes.
 */
export function useWorkCenters(tenantCode: string): UseQueryResult<WorkCenter[]> {
  return useQuery(
    (signal) => listWorkCenters(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar centros de trabalho'
  );
}
