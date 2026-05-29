import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Carrier, ShipmentOrder, Dispatch } from '../types';

const BASE = '/api/logistics';

export async function createCarrier(tenantCode: string, body: {
  name: string; documentNumber: string; contactEmail?: string;
  ratePerKg: number; ratePerCbm: number;
}): Promise<Carrier> {
  return fetchJsonOrThrow<Carrier>(
    `${BASE}/carriers`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar transportadora'
  );
}

export async function activateCarrier(tenantCode: string, id: string): Promise<void> {
  const r = await fetch(`${BASE}/carriers/${id}/activate`, { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) });
  if (!r.ok) throw new Error(await r.text());
}

export async function deactivateCarrier(tenantCode: string, id: string): Promise<void> {
  const r = await fetch(`${BASE}/carriers/${id}/deactivate`, { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) });
  if (!r.ok) throw new Error(await r.text());
}

export async function createShipmentOrder(tenantCode: string, body: {
  productionOrderRef?: string; notes?: string;
}): Promise<ShipmentOrder> {
  return fetchJsonOrThrow<ShipmentOrder>(
    `${BASE}/shipment-orders`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar ordem de expedição'
  );
}

export async function transitionShipmentOrder(tenantCode: string, id: string, action: string): Promise<void> {
  const r = await fetch(`${BASE}/shipment-orders/${id}/${action}`, { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) });
  if (!r.ok) throw new Error(await r.text());
}

export async function createDispatch(tenantCode: string, body: {
  shipmentOrderId: string; carrierId: string; vehicleId?: string; driverPersonId?: string;
}): Promise<Dispatch> {
  return fetchJsonOrThrow<Dispatch>(
    `${BASE}/dispatches`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao criar despacho'
  );
}

export async function transitionDispatch(tenantCode: string, id: string, action: string): Promise<void> {
  const r = await fetch(`${BASE}/dispatches/${id}/${action}`, { method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode) });
  if (!r.ok) throw new Error(await r.text());
}
