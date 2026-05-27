import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { ProductionDashboard, InventoryDashboard } from '../types';

export const getProductionDashboard = (tenantCode: string, signal?: AbortSignal): Promise<ProductionDashboard> =>
  fetchJsonOrThrow<ProductionDashboard>(
    '/api/production/dashboard',
    { headers: buildTenantHeaders(tenantCode), credentials: 'include', signal },
    'Falha ao carregar dados de produção'
  );

export const getInventoryDashboard = (tenantCode: string, signal?: AbortSignal): Promise<InventoryDashboard> =>
  fetchJsonOrThrow<InventoryDashboard>(
    '/api/inventory/dashboard',
    { headers: buildTenantHeaders(tenantCode), credentials: 'include', signal },
    'Falha ao carregar dados de estoque'
  );
