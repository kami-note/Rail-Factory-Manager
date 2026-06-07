import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Vehicle, DriverAssignment } from '../types';

const base = '/api/fleet';

// --- Vehicles ---

export const listVehicles = (tenantCode: string, signal?: AbortSignal): Promise<Vehicle[]> =>
  fetchJsonOrThrow<Vehicle[]>(`${base}/vehicles`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar veículos');

export const getVehicle = (tenantCode: string, id: string): Promise<Vehicle> =>
  fetchJsonOrThrow<Vehicle>(`${base}/vehicles/${id}`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include'
  }, 'Falha ao carregar veículo');

export const createVehicle = (
  tenantCode: string,
  payload: {
    plate: string; chassis: string; renavam: string; rntrc?: string; type: string;
    maxWeightKg: number; maxVolumeCbm: number; licenseExpiry: string;
  }
): Promise<Vehicle> =>
  fetchJsonOrThrow<Vehicle>(`${base}/vehicles`, {
    method: 'POST',
    headers: buildTenantHeaders(tenantCode),
    credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao criar veículo');

export const deactivateVehicle = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/vehicles/${id}/deactivate`, {
    method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}'
  }, 'Falha ao inativar veículo');

export const activateVehicle = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/vehicles/${id}/activate`, {
    method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}'
  }, 'Falha ao ativar veículo');

// --- Driver Assignments ---

export const assignDriver = (
  tenantCode: string,
  vehicleId: string,
  payload: { driverPersonId: string; startDate: string; endDate?: string; notes?: string }
): Promise<DriverAssignment> =>
  fetchJsonOrThrow<DriverAssignment>(`${base}/vehicles/${vehicleId}/driver-assignments`, {
    method: 'POST',
    headers: buildTenantHeaders(tenantCode),
    credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao atribuir motorista');

export const listDriverAssignments = (
  tenantCode: string,
  vehicleId: string,
  signal?: AbortSignal
): Promise<DriverAssignment[]> =>
  fetchJsonOrThrow<DriverAssignment[]>(`${base}/vehicles/${vehicleId}/driver-assignments`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar alocações');
