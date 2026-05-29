import { useQuery } from '../../../shared/lib/useQuery';
import type { UseQueryResult } from '../../../shared/lib/useQuery';
import { listPeople } from '../api/hr';
import type { Person } from '../types';

export function usePeople(tenantCode: string, type?: string): UseQueryResult<Person[]> {
  return useQuery(
    (signal) => listPeople(tenantCode, type, signal),
    [tenantCode, type ?? ''],
    'Falha ao carregar pessoas'
  );
}
