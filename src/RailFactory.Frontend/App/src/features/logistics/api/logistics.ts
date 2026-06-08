import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Carrier, Dispatch, ShipmentOrder, ShipmentItem, TenantFiscalProfile } from '../types';

const BASE = '/api/logistics';

// ── Carriers ──────────────────────────────────────────────────────────────────

export async function createCarrier(tenantCode: string, body: {
  name: string; documentNumber: string; contactEmail?: string;
  webhookUrl?: string; ratePerKg: number; ratePerCbm: number;
}): Promise<Carrier> {
  return fetchJsonOrThrow<Carrier>(
    `${BASE}/carriers`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar transportadora'
  );
}

export async function listCarriers(tenantCode: string, signal?: AbortSignal): Promise<Carrier[]> {
  return fetchJsonOrThrow<Carrier[]>(
    `${BASE}/carriers`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode), signal },
    'Erro ao listar transportadoras'
  );
}

export async function activateCarrier(tenantCode: string, id: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/carriers/${id}/activate`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao ativar transportadora'
  );
}

export async function deactivateCarrier(tenantCode: string, id: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/carriers/${id}/deactivate`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao desativar transportadora'
  );
}

// ── Shipment Orders ───────────────────────────────────────────────────────────

export async function createShipmentOrder(tenantCode: string, body: {
  productionOrderRef?: string;
  notes?: string;
  recipientCnpj?: string;
  recipientName?: string;
  recipientEmail?: string;
  recipientStreet?: string;
  recipientNumber?: string;
  recipientDistrict?: string;
  recipientCity?: string;
  recipientState?: string;
  recipientZipCode?: string;
  recipientIe?: string;
  modalidadeFrete?: number;
  natureOfOperation?: string;
}): Promise<ShipmentOrder> {
  return fetchJsonOrThrow<ShipmentOrder>(
    `${BASE}/shipment-orders`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar ordem de expedição'
  );
}

export async function addShipmentItem(tenantCode: string, orderId: string, body: {
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  weightKg: number;
  volumeCbm: number;
  ncmCode?: string;
  cfopCode?: string;
  unitValue?: number;
  taxBaseIcms?: number;
  icmsRate?: number;
  icmsOrigin?: number;
  icmsCst?: string;
  pisCst?: string;
  cofinsCst?: string;
  ipiRate?: number;
  ipiCst?: string;
}): Promise<ShipmentItem> {
  return fetchJsonOrThrow<ShipmentItem>(
    `${BASE}/shipment-orders/${orderId}/items`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao adicionar item'
  );
}

export async function listShipmentOrders(tenantCode: string, signal?: AbortSignal): Promise<ShipmentOrder[]> {
  return fetchJsonOrThrow<ShipmentOrder[]>(
    `${BASE}/shipment-orders`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode), signal },
    'Erro ao listar ordens de expedição'
  );
}

export async function transitionShipmentOrder(tenantCode: string, id: string, action: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/shipment-orders/${id}/${action}`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao atualizar ordem de expedição'
  );
}

// ── Dispatches ────────────────────────────────────────────────────────────────

export async function createDispatch(tenantCode: string, body: {
  shipmentOrderId: string; carrierId: string; vehicleId: string; driverPersonId: string;
  vehiclePlate?: string; vehicleRntrc?: string; driverCpf?: string; driverName?: string;
}): Promise<Dispatch> {
  return fetchJsonOrThrow<Dispatch>(
    `${BASE}/dispatches`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar despacho'
  );
}

export async function listDispatches(tenantCode: string, signal?: AbortSignal): Promise<Dispatch[]> {
  return fetchJsonOrThrow<Dispatch[]>(
    `${BASE}/dispatches`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode), signal },
    'Erro ao listar despachos'
  );
}

export async function transitionDispatch(tenantCode: string, id: string, action: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/dispatches/${id}/${action}`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao atualizar despacho'
  );
}

export async function retryFiscalEmission(tenantCode: string, dispatchId: string): Promise<void> {
  return fetchJsonOrThrow<void>(
    `${BASE}/dispatches/${dispatchId}/retry-fiscal`,
    { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao reemitir NF-e'
  );
}

export async function issueFiscalDocument(tenantCode: string, dispatchId: string, body: {
  natureOfOperation: string;
  emitter: { cnpjOrCpf: string; name: string; email: string; address: { street: string; number: string; complement?: string; district: string; city: string; state: string; zipCode: string }; ieStateRegistration?: string };
  recipient: { cnpjOrCpf: string; name: string; email: string; address: { street: string; number: string; complement?: string; district: string; city: string; state: string; zipCode: string } };
  items: Array<{ code: string; description: string; ncmCode: string; cfopCode: string; unitOfMeasure: string; quantity: number; unitValue: number; taxBaseIcms: number; icmsRate: number }>;
}): Promise<{ externalId: string; status: string; accessKey?: string; errorMessage?: string }> {
  return fetchJsonOrThrow(
    `${BASE}/dispatches/${dispatchId}/fiscal-document`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao emitir documento fiscal'
  );
}

// ── Fiscal Profile ─────────────────────────────────────────────────────────────

export async function getFiscalProfile(tenantCode: string, signal?: AbortSignal): Promise<TenantFiscalProfile | null> {
  const res = await fetch(`${BASE}/fiscal-profile`, {
    credentials: 'include',
    headers: buildTenantHeaders(tenantCode),
    signal,
  });
  if (res.status === 204) return null;
  if (!res.ok) throw new Error('Erro ao carregar perfil fiscal');
  return res.json();
}

export async function upsertFiscalProfile(tenantCode: string, body: Omit<TenantFiscalProfile, 'updatedAt'>): Promise<TenantFiscalProfile> {
  return fetchJsonOrThrow<TenantFiscalProfile>(
    `${BASE}/fiscal-profile`,
    { method: 'PUT', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao salvar perfil fiscal'
  );
}
