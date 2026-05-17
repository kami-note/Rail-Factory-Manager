import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { ProductionDashboard, InventoryDashboard } from '../types';

export const getProductionDashboard = (tenantCode: string): Promise<ProductionDashboard> =>
  fetchJsonOrThrow<ProductionDashboard>(
    '/api/production/dashboard',
    { headers: buildTenantHeaders(tenantCode), credentials: 'include' },
    'Falha ao carregar dados de produção'
  );

export const getInventoryDashboard = (tenantCode: string): Promise<InventoryDashboard> =>
  fetchJsonOrThrow<InventoryDashboard>(
    '/api/inventory/dashboard',
    { headers: buildTenantHeaders(tenantCode), credentials: 'include' },
    'Falha ao carregar dados de estoque'
  );
