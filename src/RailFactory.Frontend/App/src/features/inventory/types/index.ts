import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

export type InventoryBalance = {
  id: string;
  materialCode: string;
  materialName: string;
  quantity: number;
  unitOfMeasure: string;
  status: DisplayStatus;
  sourceReference: string;
  lotNumber?: string;
  expirationDate?: string;
  sourceType: DisplayStatus;
  supplierName?: string;
  materialImageUrl?: string;
  createdAt: string;
  ncm?: string;
  gtin?: string;
  productionOrderNumber?: string;
};

export type MaterialSearchResult = {
  materialCode: string;
  officialName: string;
  description: string;
  imageUrl?: string;
  category: string;
  gtin?: string;
  ncm?: string;
  unitOfMeasure: string;
};
