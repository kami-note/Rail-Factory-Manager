import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

export type Status = {
  service: string;
  environment: string;
  tenant: {
    code: string;
  };
};

export type Receipt = {
  id: string;
  receiptNumber: string;
  documentNumber: string;
  supplierName: string;
  issuedAt: string;
  accessKey?: string;
  totalValue?: number;
  status: DisplayStatus;
  itemCount: number;
};

export type PendingBalance = {
  id: string;
  materialCode: string;
  materialName: string;
  quantity: number;
  unitOfMeasure: string;
  status: DisplayStatus;
  sourceReference: string;
  lotNumber?: string;
  expirationDate?: string;
  sourceType: string;
  supplierName?: string;
  materialImageUrl?: string;
  createdAt: string;
  ncm?: string;
  gtin?: string;
};

export type ConferenceItem = {
  id: string;
  materialCode: string;
  unitOfMeasure: string;
  originalDescription?: string;
  imageUrl?: string;
};

export type AssociationQueueItem = {
  receiptId: string;
  receiptNumber: string;
  supplierName: string;
  documentNumber: string;
  issuedAt: string;
  status: string;
  totalItems: number;
  resolvedItems: number;
  blockingItems: number;
};

export type WorkbenchSuggestion = {
  materialCode: string;
  officialName: string;
  stockUnit: string;
  confidence: 'High' | 'Medium' | 'Low';
  reason: string;
};

export type WorkbenchItem = {
  itemId: string;
  version: string;
  associationStatus: 'Pending' | 'Mapped' | 'CreatedAndMapped' | 'ReviewLater' | 'Ignored' | 'Conflict';
  supplierProductCode: string;
  description: string;
  originalDescription?: string;
  ncm?: string;
  gtin?: string;
  supplierUnit: string;
  quantity: number;
  unitPrice?: number;
  internalMaterialCode?: string;
  internalMaterialName?: string;
  stockUnit?: string;
  conversionFactor?: number;
  reviewReason?: string;
  suggestions: WorkbenchSuggestion[];
};

export type AssociationWorkbench = {
  receipt: {
    id: string;
    receiptNumber: string;
    version: string;
    supplierFiscalId: string;
    supplierName: string;
    status: string;
    canReleaseToConference: boolean;
    releaseBlockers: string[];
  };
  items: WorkbenchItem[];
};

export type AssociateReceiptItemResponse = {
  itemId: string;
  version: string;
  associationStatus: string;
  internalMaterialCode?: string;
  conversionFactor?: number;
  canReleaseReceiptToConference: boolean;
};

export type CreateMaterialAndAssociateRequest = {
  expectedVersion: string;
  materialCode: string;
  officialName: string;
  description: string;
  originalDescription?: string;
  unitOfMeasure: string;
  procurementType: string;
  category: string;
  gtin?: string;
  ncm?: string;
  conversionFactor: number;
};
