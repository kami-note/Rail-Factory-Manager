import { useQuery } from '../../../shared/lib/useQuery';
import { listShipmentOrders } from '../api/logistics';
import type { ShipmentOrder } from '../types';

export function useShipmentOrders(tenantCode: string) {
  return useQuery<ShipmentOrder[]>(
    (signal) => listShipmentOrders(tenantCode, signal),
    [tenantCode],
    'Falha ao carregar ordens de expedição'
  );
}
