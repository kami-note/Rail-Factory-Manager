import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { 
  AssociationWorkbench, 
  AssociationQueueItem, 
  AssociateReceiptItemResponse,
  CreateMaterialAndAssociateRequest
} from '../types';

export const getAssociationQueue = async (tenantCode: string): Promise<AssociationQueueItem[]> => {
  return fetchJsonOrThrow<AssociationQueueItem[]>(
    '/api/supply-chain/receipts/association-queue',
    {
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include'
    },
    'Não foi possível carregar a fila de associação'
  );
};

export const getAssociationWorkbench = async (tenantCode: string, receiptId: string): Promise<AssociationWorkbench> => {
  return fetchJsonOrThrow<AssociationWorkbench>(
    `/api/supply-chain/receipts/${receiptId}/association-workbench`,
    {
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include'
    },
    'Não foi possível carregar os detalhes da bancada'
  );
};

export const associateReceiptItem = async (
  tenantCode: string, 
  receiptId: string, 
  itemId: string, 
  payload: { expectedVersion: string; internalMaterialCode: string; conversionFactor: number }
): Promise<AssociateReceiptItemResponse> => {
  return fetchJsonOrThrow<AssociateReceiptItemResponse>(
    `/api/supply-chain/receipts/${receiptId}/items/${itemId}/association`,
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include',
      body: JSON.stringify(payload)
    },
    'Não foi possível associar o item do recebimento'
  );
};

export const createMaterialAndAssociate = async (
  tenantCode: string,
  receiptId: string,
  itemId: string,
  payload: CreateMaterialAndAssociateRequest
): Promise<AssociateReceiptItemResponse> => {
  return fetchJsonOrThrow<AssociateReceiptItemResponse>(
    `/api/supply-chain/receipts/${receiptId}/items/${itemId}/create-material-and-associate`,
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include',
      body: JSON.stringify(payload)
    },
    'Não foi possível criar o material e associar o item'
  );
};

export const recordControlledDecision = async (
  tenantCode: string,
  receiptId: string,
  itemId: string,
  decision: 'review-later' | 'ignored',
  payload: { expectedVersion: string; reason: string }
): Promise<AssociateReceiptItemResponse> => {
  return fetchJsonOrThrow<AssociateReceiptItemResponse>(
    `/api/supply-chain/receipts/${receiptId}/items/${itemId}/${decision}`,
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include',
      body: JSON.stringify(payload)
    },
    decision === 'review-later'
      ? 'Não foi possível registrar a revisão posterior'
      : 'Não foi possível ignorar este item'
  );
};

export const overrideSupplierProductCode = async (
  tenantCode: string,
  receiptId: string,
  itemId: string,
  payload: { expectedVersion: string; correctedCode: string; reason: string }
): Promise<AssociateReceiptItemResponse> => {
  return fetchJsonOrThrow<AssociateReceiptItemResponse>(
    `/api/supply-chain/receipts/${receiptId}/items/${itemId}/override-supplier-sku`,
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include',
      body: JSON.stringify(payload)
    },
    'Não foi possível corrigir o SKU do fornecedor'
  );
};

export const releaseToConference = async (
  tenantCode: string,
  receiptId: string,
  payload: { expectedVersion: string }
): Promise<{ receiptId: string; status: string }> => {
  return fetchJsonOrThrow<{ receiptId: string; status: string }>(
    `/api/supply-chain/receipts/${receiptId}/release-to-conference`,
    {
      method: 'POST',
      headers: buildTenantHeaders(tenantCode),
      credentials: 'include',
      body: JSON.stringify(payload)
    },
    'Não foi possível liberar o recebimento para conferência'
  );
};
