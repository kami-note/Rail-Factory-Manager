import type { DisplayStatus, StatusColor } from '../../../shared/lib/utils/status-mapping';

export type VehicleType = {
  key: 'car' | 'truck' | 'van' | 'motorcycle';
  label: string;
  color: StatusColor;
};

export type Vehicle = {
  id: string;
  plate: string;
  chassis: string;
  renavam: string;
  maxWeightKg: number;
  maxVolumeCbm: number;
  licenseExpiry: string;
  type: VehicleType;
  status: DisplayStatus;
  createdAt: string;
  updatedAt: string;
};

export type DriverAssignment = {
  id: string;
  vehicleId: string;
  driverPersonId: string;
  startDate: string;
  endDate?: string;
  notes?: string;
  assignedAt: string;
};
