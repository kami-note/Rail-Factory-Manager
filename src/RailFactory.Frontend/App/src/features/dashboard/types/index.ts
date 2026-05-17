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

export type ProductionDashboard = {
  ordersByStatus: Record<string, number>;
  activeOrders: number;
  topScrap: MaterialScrapSummary[];
  inspectionSummary: InspectionSummary;
};

export type InventoryDashboard = {
  totalMaterials: number;
  materialsWithStock: number;
  availableCount: number;
  reservedCount: number;
};
