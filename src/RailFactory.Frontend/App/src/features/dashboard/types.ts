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
  receiptDate: string;
  status: string;
  createdAt: string;
  itemCount: number;
};

export type Supplier = {
  id: string;
  fiscalId: string;
  name: string;
};

export type PendingBalance = {
  id: string;
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  status: string;
  sourceReference: string;
  createdAt: string;
};
