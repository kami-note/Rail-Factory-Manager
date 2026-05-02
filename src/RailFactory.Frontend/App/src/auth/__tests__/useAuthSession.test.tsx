import { renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useAuthSession } from '../useAuthSession';

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

    const { result } = renderHook(() => useAuthSession('dev'));

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

    const { result } = renderHook(() => useAuthSession('dev'));

    await waitFor(() => {
      expect(result.current.status).toBe('unauthenticated');
    });

    expect(result.current.oauthError).toContain('oauth_error');
  });
});
