import type { DisplayStatus, StatusColor } from '../../../shared/lib/utils/status-mapping';

export type PersonType = {
  key: 'employee' | 'driver' | 'contractor';
  label: string;
  color: StatusColor;
};

export type Person = {
  id: string;
  name: string;
  documentNumber: string;
  email?: string;
  type: PersonType;
  status: DisplayStatus;
  createdAt: string;
  updatedAt: string;
};

export type HourLog = {
  id: string;
  personId: string;
  date: string;
  hoursWorked: number;
  description?: string;
  recordedAt: string;
};
