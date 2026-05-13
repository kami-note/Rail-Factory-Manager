import { renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { usePermissions } from '../hooks/usePermissions';
import * as authSession from '../hooks/useAuthSession';

vi.mock('../hooks/useAuthSession', () => ({
  useAuthSession: vi.fn(),
}));

describe('usePermissions', () => {
  it('returns true when user has required permission', () => {
    vi.mocked(authSession.useAuthSession).mockReturnValue({
      status: 'authenticated',
      session: {
        authenticated: true,
        user: {
          permissions: ['inventory.read', 'supplychain.write'],
        },
      },
    } as any);

    const { result } = renderHook(() => usePermissions());

    expect(result.current.hasPermission('inventory.read')).toBe(true);
    expect(result.current.hasPermission('production.read')).toBe(false);
  });

  it('correctly handles hasAnyPermission', () => {
    vi.mocked(authSession.useAuthSession).mockReturnValue({
      status: 'authenticated',
      session: {
        authenticated: true,
        user: {
          permissions: ['inventory.read'],
        },
      },
    } as any);

    const { result } = renderHook(() => usePermissions());

    expect(result.current.hasAnyPermission(['inventory.read', 'production.read'])).toBe(true);
    expect(result.current.hasAnyPermission(['supplychain.read', 'production.read'])).toBe(false);
  });

  it('correctly handles hasAllPermissions', () => {
    vi.mocked(authSession.useAuthSession).mockReturnValue({
      status: 'authenticated',
      session: {
        authenticated: true,
        user: {
          permissions: ['inventory.read', 'inventory.write'],
        },
      },
    } as any);

    const { result } = renderHook(() => usePermissions());

    expect(result.current.hasAllPermissions(['inventory.read', 'inventory.write'])).toBe(true);
    expect(result.current.hasAllPermissions(['inventory.read', 'production.read'])).toBe(false);
  });

  it('returns false for everything when user is unauthenticated', () => {
    vi.mocked(authSession.useAuthSession).mockReturnValue({
      status: 'unauthenticated',
      session: { authenticated: false },
    } as any);

    const { result } = renderHook(() => usePermissions());

    expect(result.current.permissions).toEqual([]);
    expect(result.current.hasPermission('inventory.read')).toBe(false);
  });
});
