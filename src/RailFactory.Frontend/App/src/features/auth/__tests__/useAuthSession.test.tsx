import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useAuthSession } from '../hooks/useAuthSession';
import { AuthSessionProvider } from '../context/AuthSessionContext';

function createWrapper(tenantCode: string) {
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <AuthSessionProvider tenantCode={tenantCode}>{children}</AuthSessionProvider>;
  };
}

describe('useAuthSession', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('keeps loading until session response resolves, then becomes unauthenticated', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        status: 401,
        ok: false,
        json: async () => ({ authenticated: false })
      })
    );

    const { result } = renderHook(() => useAuthSession('dev'), { wrapper: createWrapper('dev') });

    expect(result.current.status).toBe('loading');

    await waitFor(() => {
      expect(result.current.status).toBe('unauthenticated');
    });
  });

  it('surfaces OAuth errors from querystring and keeps safe unauthenticated state', async () => {
    window.history.pushState(null, '', '/app?oauth=error&error=oauth_error');

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        status: 401,
        ok: false,
        json: async () => ({ authenticated: false })
      })
    );

    const { result } = renderHook(() => useAuthSession('dev'), { wrapper: createWrapper('dev') });

    await waitFor(() => {
      expect(result.current.status).toBe('unauthenticated');
    });

    expect(result.current.oauthError).toContain('oauth_error');
  });

  it('keeps unauthenticated state and skips network call when tenant is empty', async () => {
    const fetchSpy = vi.fn();
    vi.stubGlobal('fetch', fetchSpy);

    const { result } = renderHook(() => useAuthSession(''), { wrapper: createWrapper('') });

    await waitFor(() => {
      expect(result.current.status).toBe('unauthenticated');
    });

    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
