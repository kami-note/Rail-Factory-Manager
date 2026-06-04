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
  | string;

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
  conferencedAt?: string;
  dispatchedAt?: string;
  deliveredAt?: string;
  createdAt: string;
}
