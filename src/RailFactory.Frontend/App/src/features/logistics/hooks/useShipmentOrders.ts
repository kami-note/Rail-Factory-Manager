import { useQuery } from '../../../shared/lib/useQuery';
import { buildTenantHeaders } from '../../../shared/lib/http';
import type { ShipmentOrder } from '../types';

export function useShipmentOrders(tenantCode: string) {
  return useQuery<ShipmentOrder[]>(
    (signal) =>
      fetch('/api/logistics/shipment-orders', {
        signal,
        credentials: 'include',
        headers: buildTenantHeaders(tenantCode),
      }).then((r) => (r.ok ? r.json() : Promise.reject(new Error(r.statusText)))),
    [tenantCode],
    'Falha ao carregar ordens de expedição'
  );
}
