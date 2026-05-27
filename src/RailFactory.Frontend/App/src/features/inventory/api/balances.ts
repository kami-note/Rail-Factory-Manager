import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { InventoryBalance } from '../types';

/**
 * Fetches all inventory balances for the current tenant.
 */
export const listBalances = (
  tenantCode: string,
  sourceType?: 'Purchase' | 'Production',
  signal?: AbortSignal
): Promise<InventoryBalance[]> => {
  const params = sourceType ? `?sourceType=${sourceType}` : '';
  return fetchJsonOrThrow<InventoryBalance[]>(
    `/api/inventory/balances${params}`,
    { headers: buildTenantHeaders(tenantCode), credentials: 'include', signal },
    'Falha ao carregar saldos de estoque'
  );
};
