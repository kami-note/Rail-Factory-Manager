import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listVehicles } from '../api/fleet';
import type { Vehicle } from '../types';

export function useVehicles(tenantCode: string): UseQueryResult<Vehicle[]> {
  return useQuery(
    (signal) => listVehicles(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar veículos'
  );
}
