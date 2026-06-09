import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

export type WorkCenter = {
  id: string;
  code: string;
  name: string;
  /** DisplayStatus object from the backend — use StatusChip to render. */
  status: DisplayStatus;
  createdAt: string;
  updatedAt: string;
};

export type BomItem = {
  id: string;
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  scrapFactor: number;
};

export type Bom = {
  id: string;
  productCode: string;
  version: number;
  /** DisplayStatus object from the backend — use StatusChip to render. */
  status: DisplayStatus;
  batchSize: number;
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
  /** DisplayStatus object from the backend — use StatusChip to render. */
  status: DisplayStatus;
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
