/**
 * Mock data for the production monitoring dashboard.
 */
export const productionLines = [
  { id: 'RF-101', product: 'Chassis A1 Heavy', priority: 'High', status: 'Active', color: 'success.main' },
  { id: 'RF-102', product: 'Brake System v2', priority: 'Normal', status: 'Idle', color: 'text.secondary' },
  { id: 'RF-103', product: 'Control Unit C', priority: 'Critical', status: 'Warning', color: 'error.main' },
  { id: 'RF-104', product: 'Wheel Set 18in', priority: 'Low', status: 'Active', color: 'success.main' },
  { id: 'RF-105', product: 'Battery Pack L2', priority: 'High', status: 'Active', color: 'success.main' },
];

/**
 * Mock data for the activity log.
 */
export const activityLogs = [
  { time: '10:45:02', msg: 'Batch RCPT-104 validation completed', type: 'success' },
  { time: '10:30:15', msg: 'Inbound Line #2 sensor recalibrated', type: 'info' },
  { time: '09:55:40', msg: 'System check: Storage capacity at 88%', type: 'warning' },
  { time: '08:45:12', msg: 'Shift handover successful (Team B)', type: 'info' }
];
