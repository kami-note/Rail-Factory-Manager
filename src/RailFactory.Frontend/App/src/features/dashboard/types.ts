export type Status = {
  service: string;
  environment: string;
  tenant: {
    code: string;
  };
  gateway: unknown;
};

export type Receipt = {
  id: string;
  receiptNumber: string;
  documentNumber: string;
  accessKey?: string;
  totalValue?: number;
  status: string;
  itemCount: number;
  createdAt: string;
};

export type PendingBalance = {
  id: string;
  materialCode: string;
  materialName: string;
  quantity: number;
  unitOfMeasure: string;
  status: string;
  sourceReference: string;
  lotNumber?: string;
  expirationDate?: string;
  sourceType: string;
  supplierName?: string;
  materialImageUrl?: string;
  sourceMetadata?: string;
  createdAt: string;
  ncm?: string;
  gtin?: string;
};

export type ConferenceItem = {
  id: string;
  materialCode: string;
  unitOfMeasure: string;
  originalDescription?: string;
};
