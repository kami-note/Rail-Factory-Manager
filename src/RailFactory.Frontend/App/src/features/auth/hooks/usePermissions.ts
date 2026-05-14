import { useAuthSession } from './useAuthSession';

/**
 * Hook to manage and check atomic permissions for the current user.
 * Provides functions to validate if the user has specific access rights.
 */
export const usePermissions = (tenantCode?: string) => {
  const { session } = useAuthSession(tenantCode || '');
  const permissions = session.user?.permissions ?? [];

  /**
   * Checks if the user has a specific permission.
   * @param permission - The permission code to check (e.g., 'inventory.read').
   */
  const hasPermission = (permission: string): boolean => {
    return permissions.includes(permission);
  };

  /**
   * Checks if the user has ALL of the specified permissions.
   * @param requiredPermissions - Array of permission codes.
   */
  const hasAllPermissions = (requiredPermissions: string[]): boolean => {
    return requiredPermissions.every((p) => permissions.includes(p));
  };

  /**
   * Checks if the user has AT LEAST ONE of the specified permissions.
   * @param requiredPermissions - Array of permission codes.
   */
  const hasAnyPermission = (requiredPermissions: string[]): boolean => {
    return requiredPermissions.some((p) => permissions.includes(p));
  };

  return {
    permissions,
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
  };
};
