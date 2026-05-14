import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { MaterialSearchResult } from '../types';

/**
 * Searches the material catalog by name, code or GTIN.
 */
export const searchMaterials = async (
  tenantCode: string, 
  query: string
): Promise<MaterialSearchResult[]> => {
  if (query.length < 2) return [];

  return fetchJsonOrThrow<MaterialSearchResult[]>(
    `/api/inventory/materials/search?q=${encodeURIComponent(query)}`,
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
