import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

const BASE = '/api/fleet';

export type MaintenanceType = 'Preventive' | 'Corrective';
export type MaintenanceStatus = 'Scheduled' | 'Done' | 'Cancelled';

export interface MaintenancePlan {
  id: string;
  vehicleId: string;
  type: MaintenanceType;
  description: string;
  scheduledDate: string;
  completedDate?: string;
  status: MaintenanceStatus;
  notes?: string;
  createdAt: string;
}

export interface FuelingRecord {
  id: string;
  vehicleId: string;
  date: string;
  litersSupplied: number;
  pricePerLiter: number;
  totalBrl: number;
  odometer?: number;
  supplier?: string;
  notes?: string;
  recordedAt: string;
}

export async function listMaintenancePlans(tenantCode: string, vehicleId: string): Promise<MaintenancePlan[]> {
  return fetchJsonOrThrow<MaintenancePlan[]>(
    `${BASE}/vehicles/${vehicleId}/maintenance-plans`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao listar manutenções'
  );
}

export async function scheduleMaintenance(tenantCode: string, vehicleId: string, body: {
  type: MaintenanceType; description: string; scheduledDate: string; notes?: string;
}): Promise<MaintenancePlan> {
  return fetchJsonOrThrow<MaintenancePlan>(
    `${BASE}/vehicles/${vehicleId}/maintenance-plans`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao agendar manutenção'
  );
}

export async function completeMaintenance(tenantCode: string, vehicleId: string, planId: string, completedDate: string): Promise<void> {
  const r = await fetch(`${BASE}/vehicles/${vehicleId}/maintenance-plans/${planId}/complete`, {
    method: 'PUT', credentials: 'include',
    headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' },
    body: JSON.stringify({ completedDate }),
  });
  if (!r.ok) throw new Error(await r.text());
}

export async function cancelMaintenance(tenantCode: string, vehicleId: string, planId: string): Promise<void> {
  const r = await fetch(`${BASE}/vehicles/${vehicleId}/maintenance-plans/${planId}/cancel`, {
    method: 'PUT', credentials: 'include', headers: buildTenantHeaders(tenantCode),
  });
  if (!r.ok) throw new Error(await r.text());
}

export async function listFuelingRecords(tenantCode: string, vehicleId: string): Promise<FuelingRecord[]> {
  return fetchJsonOrThrow<FuelingRecord[]>(
    `${BASE}/vehicles/${vehicleId}/fueling-records`,
    { credentials: 'include', headers: buildTenantHeaders(tenantCode) },
    'Erro ao listar abastecimentos'
  );
}

export async function recordFueling(tenantCode: string, vehicleId: string, body: {
  date: string; litersSupplied: number; pricePerLiter: number; odometer?: number; supplier?: string; notes?: string;
}): Promise<FuelingRecord> {
  return fetchJsonOrThrow<FuelingRecord>(
    `${BASE}/vehicles/${vehicleId}/fueling-records`,
    { method: 'POST', credentials: 'include', headers: { ...buildTenantHeaders(tenantCode), 'Content-Type': 'application/json' }, body: JSON.stringify(body) },
    'Erro ao registrar abastecimento'
  );
}
