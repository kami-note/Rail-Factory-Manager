export type WorkCenter = {
  id: string;
  code: string;
  name: string;
  status: 'Active' | 'Inactive';
  createdAt: string;
  updatedAt: string;
};

export type BomItem = {
  id: string;
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
};

export type Bom = {
  id: string;
  productCode: string;
  version: number;
  status: 'Draft' | 'Active';
  items: BomItem[];
  createdAt: string;
  updatedAt: string;
};

export type ProductionOrder = {
  id: string;
  orderNumber: string;
  productCode: string;
  bomId: string;
  workCenterId: string;
  plannedQuantity: number;
  status: 'Draft' | 'Released' | 'InExecution' | 'Completed' | 'Cancelled';
  createdAt: string;
  updatedAt: string;
};

export type ConsumptionRecord = {
  materialCode: string;
  consumedQuantity: number;
  unitOfMeasure: string;
  recordedAt: string;
};

export type ScrapRecord = {
  materialCode: string;
  scrapQuantity: number;
  unitOfMeasure: string;
  reason: string;
  recordedAt: string;
};

export type InspectionRecord = {
  result: 'Passed' | 'Failed';
  inspectedBy: string;
  notes?: string;
  inspectedAt: string;
};

export type OrderExecutionHistory = {
  consumptions: ConsumptionRecord[];
  scraps: ScrapRecord[];
  inspections: InspectionRecord[];
};
