import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Person, HourLog } from '../types';

const base = '/api/hr';

// --- People ---

export const listPeople = (tenantCode: string, type?: string, signal?: AbortSignal): Promise<Person[]> => {
  const params = new URLSearchParams();
  if (type) params.set('type', type);
  const qs = params.toString();
  return fetchJsonOrThrow<Person[]>(`${base}/people${qs ? `?${qs}` : ''}`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar pessoas');
};

export const getPerson = (tenantCode: string, id: string): Promise<Person> =>
  fetchJsonOrThrow<Person>(`${base}/people/${id}`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include'
  }, 'Falha ao carregar pessoa');

export const createPerson = (
  tenantCode: string,
  payload: { name: string; documentNumber: string; type: string; email?: string }
): Promise<Person> =>
  fetchJsonOrThrow<Person>(`${base}/people`, {
    method: 'POST',
    headers: buildTenantHeaders(tenantCode),
    credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao criar pessoa');

export const deactivatePerson = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/people/${id}/deactivate`, {
    method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}'
  }, 'Falha ao inativar pessoa');

export const activatePerson = (tenantCode: string, id: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/people/${id}/activate`, {
    method: 'PUT', headers: buildTenantHeaders(tenantCode), credentials: 'include', body: '{}'
  }, 'Falha ao ativar pessoa');

// --- Hour Logs ---

export const logHours = (
  tenantCode: string,
  personId: string,
  payload: { date: string; hoursWorked: number; description?: string }
): Promise<HourLog> =>
  fetchJsonOrThrow<HourLog>(`${base}/people/${personId}/hour-logs`, {
    method: 'POST',
    headers: buildTenantHeaders(tenantCode),
    credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao registrar horas');

export const listHourLogs = (
  tenantCode: string,
  personId: string,
  signal?: AbortSignal
): Promise<HourLog[]> =>
  fetchJsonOrThrow<HourLog[]>(`${base}/people/${personId}/hour-logs`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar apontamentos');
