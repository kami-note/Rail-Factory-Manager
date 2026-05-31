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
}

export interface ShipmentOrder {
  id: string;
  orderNumber: string;
  productionOrderRef?: string;
  notes?: string;
  status: ShipmentOrderStatus;
  createdAt: string;
  updatedAt: string;
  items: ShipmentItem[];
}

export type DispatchStatus = 'Pending' | 'InTransit' | 'Delivered' | 'Returned';

export interface Dispatch {
  id: string;
  shipmentOrderId: string;
  carrierId: string;
  vehicleId?: string;
  driverPersonId?: string;
  trackingCode: string;
  freightValueBrl: number;
  status: DispatchStatus;
  conferencedAt?: string;
  dispatchedAt?: string;
  deliveredAt?: string;
  createdAt: string;
}
