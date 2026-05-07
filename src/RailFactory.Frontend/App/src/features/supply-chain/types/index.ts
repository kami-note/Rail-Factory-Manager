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
