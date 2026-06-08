import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import type { Person, HourLog, Skill, WorkShift } from '../types';

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

// --- Skills ---

export const listSkills = (tenantCode: string, personId: string, signal?: AbortSignal): Promise<Skill[]> =>
  fetchJsonOrThrow<Skill[]>(`${base}/people/${personId}/skills`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar competências');

export const addSkill = (
  tenantCode: string,
  personId: string,
  payload: { skillName: string; proficiencyLevel: number; certifiedAt?: string; notes?: string }
): Promise<Skill> =>
  fetchJsonOrThrow<Skill>(`${base}/people/${personId}/skills`, {
    method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao adicionar competência');

export const removeSkill = (tenantCode: string, personId: string, skillId: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/people/${personId}/skills/${skillId}`, {
    method: 'DELETE', headers: buildTenantHeaders(tenantCode), credentials: 'include'
  }, 'Falha ao remover competência');

// --- Shifts ---

export const listShifts = (tenantCode: string, personId: string, signal?: AbortSignal): Promise<WorkShift[]> =>
  fetchJsonOrThrow<WorkShift[]>(`${base}/people/${personId}/shifts`, {
    headers: buildTenantHeaders(tenantCode), credentials: 'include', signal
  }, 'Falha ao carregar turnos');

export const createShift = (
  tenantCode: string,
  personId: string,
  payload: { shiftDate: string; startTime: string; endTime: string; notes?: string }
): Promise<WorkShift> =>
  fetchJsonOrThrow<WorkShift>(`${base}/people/${personId}/shifts`, {
    method: 'POST', headers: buildTenantHeaders(tenantCode), credentials: 'include',
    body: JSON.stringify(payload)
  }, 'Falha ao criar turno');

export const deleteShift = (tenantCode: string, personId: string, shiftId: string): Promise<void> =>
  fetchJsonOrThrow<void>(`${base}/people/${personId}/shifts/${shiftId}`, {
    method: 'DELETE', headers: buildTenantHeaders(tenantCode), credentials: 'include'
  }, 'Falha ao excluir turno');

/**
 * Uploads an image file for a person profile.
 */
export const uploadPersonImage = async (
  tenantCode: string,
  personId: string,
  file: File
): Promise<{ id: string; imageUrl: string }> => {
  const formData = new FormData();
  formData.append('file', file);

  return fetchJsonOrThrow<{ id: string; imageUrl: string }>(
    `/api/hr/people/${personId}/image`,
    {
      method: 'PUT',
      headers: buildTenantHeaders(tenantCode),
      body: formData,
      credentials: 'include'
    },
    'Falha ao enviar imagem do funcionário'
  );
};
