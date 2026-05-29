/**
 * Central registry of all atomic permissions available in the system.
 * This file MUST be kept in sync with RailFactory.BuildingBlocks.Auth.SystemPermissions.cs.
 */
export const SystemPermissions = {
  Inventory: {
    Read: 'inventory.read',
    Write: 'inventory.write',
    Delete: 'inventory.delete',
  },
  SupplyChain: {
    Read: 'supplychain.read',
    Write: 'supplychain.write',
    Admin: 'supplychain.admin',
  },
  Production: {
    Read: 'production.read',
    Write: 'production.write',
  },
  Iam: {
    Read: 'iam.read',
    Write: 'iam.write',
    RolesManage: 'iam.roles.manage',
  },
  Hr: {
    Read: 'hr.read',
    Write: 'hr.write',
  },
  Fleet: {
    Read: 'fleet.read',
    Write: 'fleet.write',
  },
  Logistics: {
    Read: 'logistics.read',
    Write: 'logistics.write',
  },
} as const;

export type SystemPermission =
  | (typeof SystemPermissions.Inventory)[keyof typeof SystemPermissions.Inventory]
  | (typeof SystemPermissions.SupplyChain)[keyof typeof SystemPermissions.SupplyChain]
  | (typeof SystemPermissions.Production)[keyof typeof SystemPermissions.Production]
  | (typeof SystemPermissions.Iam)[keyof typeof SystemPermissions.Iam]
  | (typeof SystemPermissions.Hr)[keyof typeof SystemPermissions.Hr]
  | (typeof SystemPermissions.Fleet)[keyof typeof SystemPermissions.Fleet]
  | (typeof SystemPermissions.Logistics)[keyof typeof SystemPermissions.Logistics];
