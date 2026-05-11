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
