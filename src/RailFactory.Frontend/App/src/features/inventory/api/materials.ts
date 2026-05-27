import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { MaterialSearchResult } from '../types';

export interface CreateMaterialPayload {
  materialCode: string;
  officialName: string;
  description: string;
  unitOfMeasure: string;
  procurementType: 'Buy' | 'Make';
  category: 'RawMaterial' | 'FinishedGood';
  gtin?: string;
  ncm?: string;
}

export const createMaterial = (
  tenantCode: string,
  payload: CreateMaterialPayload
): Promise<void> =>
  fetchJsonOrThrow<void>(
    '/api/inventory/materials',
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      body: JSON.stringify(payload),
      credentials: 'include',
    },
    'Falha ao cadastrar material'
  );

/**
 * Searches the material catalog by name, code or GTIN.
 */
export const searchMaterials = async (
  tenantCode: string,
  query: string,
  category?: 'RawMaterial' | 'FinishedGood'
): Promise<MaterialSearchResult[]> => {
  if (query.length < 2) return [];

  const params = new URLSearchParams({ q: query });
  if (category) params.set('category', category);

  return fetchJsonOrThrow<MaterialSearchResult[]>(
    `/api/inventory/materials/search?${params.toString()}`,
    {
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include'
    },
    'Catalog search failed'
  );
};

/**
 * Unifies a duplicate material into an official material.
 */
export const mergeMaterials = async (
  tenantCode: string,
  obsoleteMaterialCode: string,
  officialMaterialCode: string
): Promise<void> => {
  await fetchJsonOrThrow(
    '/api/inventory/materials/merge',
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      body: JSON.stringify({
        obsoleteMaterialCode,
        officialMaterialCode
      }),
      credentials: 'include'
    },
    'Falha ao unificar materiais'
  );
};
