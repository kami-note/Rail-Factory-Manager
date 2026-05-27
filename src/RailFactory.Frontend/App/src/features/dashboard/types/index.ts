export type Status = {
  service: string;
  environment: string;
  tenant: {
    code: string;
  };
};

export type MaterialScrapSummary = {
  materialCode: string;
  totalScrap: number;
  unitOfMeasure: string;
};

export type InspectionSummary = {
  passed: number;
  failed: number;
  passRate: number;
};

export type WorkCenterOrderSummary = {
  workCenterId: string;
  workCenterCode: string;
  workCenterName: string;
  totalOrders: number;
  completedOrders: number;
  completionRate: number;
};

export type ProductionDashboard = {
  ordersByStatus: Record<string, number>;
  activeOrders: number;
  topScrap: MaterialScrapSummary[];
  inspectionSummary: InspectionSummary;
  /** Average lead time in hours for Completed orders. Null when no completed orders exist. */
  averageLeadTimeHours: number | null;
  workCenterSummary: WorkCenterOrderSummary[];
};

export type InventoryDashboard = {
  totalMaterials: number;
  materialsWithStock: number;
  availableCount: number;
  reservedCount: number;
  blockedCount: number;
  /** Ratio of Available to (Available + Blocked). Null when no balance completed conference. */
  stockAccuracy: number | null;
};
