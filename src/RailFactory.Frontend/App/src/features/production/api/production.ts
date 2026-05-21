import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { WorkCenter, Bom, ProductionOrder, OrderExecutionHistory } from '../types';

const base = '/api/production';

// --- Work Centers ---

export const listWorkCenters = (tenantCode: string): Promise<WorkCenter[]> =>
  fetchJsonOrThrow<WorkCenter[]>(`${base}/work-centers`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar centros de trabalho');

export const createWorkCenter = (tenantCode: string, payload: { code: string; name: string }): Promise<WorkCenter> =>
  fetchJsonOrThrow<WorkCenter>(`${base}/work-centers`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao criar centro de trabalho');

export const deactivateWorkCenter = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/work-centers/${id}/deactivate`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao desativar centro de trabalho');

// --- BOMs ---

export const listBoms = (tenantCode: string, productCode?: string): Promise<Bom[]> => {
  const qs = productCode ? `?productCode=${encodeURIComponent(productCode)}` : '';
  return fetchJsonOrThrow<Bom[]>(`${base}/boms${qs}`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar BOMs');
};

export const getBom = (tenantCode: string, bomId: string): Promise<Bom> =>
  fetchJsonOrThrow<Bom>(`${base}/boms/${bomId}`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar BOM');

export const createBom = (tenantCode: string, payload: { productCode: string }): Promise<Bom> =>
  fetchJsonOrThrow<Bom>(`${base}/boms`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao criar BOM');

export const addBomItem = (tenantCode: string, bomId: string, payload: { materialCode: string; quantity: number; unitOfMeasure: string }): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/boms/${bomId}/items`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao adicionar item à BOM');

export const activateBom = (tenantCode: string, bomId: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/boms/${bomId}/activate`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao ativar BOM');

// --- Production Orders ---

export const listProductionOrders = (tenantCode: string, status?: string, workCenterId?: string): Promise<ProductionOrder[]> => {
  const params = new URLSearchParams();
  if (status) params.set('status', status);
  if (workCenterId) params.set('workCenterId', workCenterId);
  const qs = params.toString();
  return fetchJsonOrThrow<ProductionOrder[]>(`${base}/production-orders${qs ? `?${qs}` : ''}`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar ordens de produção');
};

export const getProductionOrder = (tenantCode: string, id: string): Promise<ProductionOrder> =>
  fetchJsonOrThrow<ProductionOrder>(`${base}/production-orders/${id}`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar ordem de produção');

export const createProductionOrder = (tenantCode: string, payload: { bomId: string; workCenterId: string; plannedQuantity: number }): Promise<ProductionOrder> =>
  fetchJsonOrThrow<ProductionOrder>(`${base}/production-orders`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao criar ordem de produção');

export const releaseProductionOrder = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${id}/release`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao liberar ordem de produção');

export const startOrderExecution = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${id}/start-execution`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao iniciar execução');

export const cancelProductionOrder = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${id}/cancel`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao cancelar ordem de produção');

export const completeProductionOrder = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${id}/complete`, { method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}' }, 'Falha ao concluir ordem de produção');

export const recordConsumption = (tenantCode: string, orderId: string, payload: { materialCode: string; consumedQuantity: number; unitOfMeasure: string; inventoryBalanceId?: string }): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${orderId}/consumptions`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao registrar consumo');

export const recordScrap = (tenantCode: string, orderId: string, payload: { materialCode: string; scrapQuantity: number; unitOfMeasure: string; reason: string }): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${orderId}/scraps`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao registrar scrap');

export const recordInspection = (tenantCode: string, orderId: string, payload: { result: 'Passed' | 'Failed'; inspectedBy: string; notes?: string }): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/production-orders/${orderId}/inspections`, { method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: JSON.stringify(payload) }, 'Falha ao registrar inspeção');

export const getOrderExecutionHistory = (tenantCode: string, orderId: string): Promise<OrderExecutionHistory> =>
  fetchJsonOrThrow<OrderExecutionHistory>(`${base}/production-orders/${orderId}/execution`, { headers: buildTenantHeaders(tenantCode), credentials: 'include' }, 'Falha ao carregar histórico de execução');
