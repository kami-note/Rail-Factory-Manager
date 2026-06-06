import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listDriverAssignments } from '../api/fleet';
import type { DriverAssignment } from '../types';

export function useDriverAssignments(tenantCode: string, vehicleId: string): UseQueryResult<DriverAssignment[]> {
  return useQuery(
    vehicleId ? (signal) => listDriverAssignments(tenantCode, vehicleId, signal) : null,
    [tenantCode, vehicleId],
    'Falha ao carregar alocações do veículo'
  );
}
