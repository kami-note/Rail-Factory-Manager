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
  createdAt: string;
  ncm?: string;
  gtin?: string;
};
