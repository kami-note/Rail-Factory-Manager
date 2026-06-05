export type CarrierStatus = 'Active' | 'Inactive';

export interface Carrier {
  id: string;
  name: string;
  documentNumber: string;
  contactEmail?: string;
  webhookUrl?: string;
  ratePerKg: number;
  ratePerCbm: number;
  status: CarrierStatus;
  createdAt: string;
  updatedAt: string;
}

export type ShipmentOrderStatus = 'Draft' | 'Picking' | 'Packing' | 'ReadyToShip' | 'Shipped' | 'Cancelled';

export interface ShipmentItem {
  id: string;
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  weightKg: number;
  volumeCbm: number;
  ncmCode?: string;
  cfopCode?: string;
  unitValue?: number;
  taxBaseIcms?: number;
  icmsRate?: number;
  icmsOrigin?: number;
  icmsCst?: string;
  pisCst?: string;
  cofinsCst?: string;
  ipiRate?: number;
}

export interface ShipmentOrder {
  id: string;
  orderNumber: string;
  productionOrderRef?: string;
  notes?: string;
  status: ShipmentOrderStatus;
  recipientCnpj?: string;
  recipientName?: string;
  recipientEmail?: string;
  recipientStreet?: string;
  recipientNumber?: string;
  recipientDistrict?: string;
  recipientCity?: string;
  recipientState?: string;
  recipientZipCode?: string;
  natureOfOperation?: string;
  createdAt: string;
  updatedAt: string;
  items: ShipmentItem[];
}

export type DispatchStatus = 'Pending' | 'InTransit' | 'Delivered' | 'Returned';

export type FiscalStatus =
  | 'processando' | 'processando_autorizacao'
  | 'autorizado' | 'CONCLUIDO'
  | 'erro_autorizacao' | 'REJEITADO'
  | 'cancelado' | 'CANCELADO'
  | 'denegado' | 'DENEGADO'
  | string; // open for future provider-specific statuses

export const FISCAL_COLOR: Record<string, 'default' | 'info' | 'success' | 'error'> = {
  processando: 'info', processando_autorizacao: 'info',
  autorizado: 'success', CONCLUIDO: 'success',
  erro_autorizacao: 'error', REJEITADO: 'error',
  cancelado: 'default', CANCELADO: 'default',
  denegado: 'error', DENEGADO: 'error',
};

export const FISCAL_LABEL: Record<string, string> = {
  processando: 'Processando', processando_autorizacao: 'Processando',
  autorizado: 'Autorizada', CONCLUIDO: 'Autorizada',
  erro_autorizacao: 'Erro', REJEITADO: 'Rejeitada',
  cancelado: 'Cancelada', CANCELADO: 'Cancelada',
  denegado: 'Denegada', DENEGADO: 'Denegada',
};

export const RETRYABLE_FISCAL_STATUSES = new Set<string>(['erro_autorizacao', 'REJEITADO']);

export interface Dispatch {
  id: string;
  shipmentOrderId: string;
  carrierId: string;
  vehicleId?: string;
  driverPersonId?: string;
  trackingCode: string;
  freightValueBrl: number;
  status: DispatchStatus;
  fiscalExternalId?: string;
  fiscalAccessKey?: string;
  fiscalStatus?: FiscalStatus;
  fiscalErrorMessage?: string;
  conferencedAt?: string;
  dispatchedAt?: string;
  deliveredAt?: string;
  createdAt: string;
}
