import { useEffect, useState } from 'react';
import { clearOAuthQueryFlag, fetchSession } from '../api/client';
import type { AuthViewState } from '../types';

export function useAuthSession(tenantCode: string) {
  const [state, setState] = useState<AuthViewState>({
    status: 'loading',
    session: { authenticated: false }
  });

  useEffect(() => {
    let active = true;

    const oauthErrorCode = clearOAuthQueryFlag();
    if (oauthErrorCode && active) {
      setState(previous => ({
        ...previous,
        oauthError: `Falha no login OAuth (${oauthErrorCode}).`
      }));
    }

    fetchSession(tenantCode)
      .then(({ payload }) => {
        if (!active) {
          return;
        }

        setState(previous => ({
          ...previous,
          session: payload,
          status: payload.authenticated ? 'authenticated' : 'unauthenticated'
        }));
      })
      .catch((requestError: Error) => {
        if (!active) {
          return;
        }

        setState(previous => ({
          ...previous,
          status: 'error',
          error: requestError.message,
          session: { authenticated: false }
        }));
      });

    return () => {
      active = false;
    };
  }, [tenantCode]);

  return state;
}
