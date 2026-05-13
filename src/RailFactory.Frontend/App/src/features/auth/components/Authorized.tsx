import React from 'react';
import { usePermissions } from '../hooks/usePermissions';

interface AuthorizedProps {
  /** The permission code required to render the children. */
  permission?: string;
  /** A list of permissions where AT LEAST ONE is required. */
  anyPermission?: string[];
  /** A list of permissions where ALL are required. */
  allPermissions?: string[];
  /** Elements to render if authorized. */
  children: React.ReactNode;
  /** Optional fallback to render if NOT authorized. */
  fallback?: React.ReactNode;
}

/**
 * A declarative wrapper to secure UI elements based on user permissions.
 * If the user does not have the required permissions, it renders nothing or a fallback.
 */
export const Authorized: React.FC<AuthorizedProps> = ({
  permission,
  anyPermission,
  allPermissions,
  children,
  fallback = null,
}) => {
  const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();

  let isAuthorized = true;

  if (permission) {
    isAuthorized = hasPermission(permission);
  } else if (anyPermission) {
    isAuthorized = hasAnyPermission(anyPermission);
  } else if (allPermissions) {
    isAuthorized = hasAllPermissions(allPermissions);
  }

  if (!isAuthorized) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
};
