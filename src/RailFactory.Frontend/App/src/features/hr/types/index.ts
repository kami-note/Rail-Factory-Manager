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
  imageUrl?: string | null;
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

export type Skill = {
  id: string;
  personId: string;
  skillName: string;
  proficiencyLevel: number;
  certifiedAt?: string;
  notes?: string;
  createdAt: string;
};

export type WorkShift = {
  id: string;
  personId: string;
  shiftDate: string;
  startTime: string;
  endTime: string;
  notes?: string;
  createdAt: string;
};
