import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { clearOAuthQueryFlag, fetchSession } from '../api/client';
import type { AuthSessionContextValue, AuthViewState } from '../types';

const AuthSessionContext = createContext<AuthSessionContextValue | null>(null);

type AuthSessionProviderProps = {
  tenantCode: string;
  children: React.ReactNode;
};

export function AuthSessionProvider({ tenantCode, children }: AuthSessionProviderProps) {
  const [state, setState] = useState<AuthViewState>({
    status: 'loading',
    session: { authenticated: false }
  });
  const [refreshNonce, setRefreshNonce] = useState(0);

  const clearSession = useCallback(() => {
    setState(previous => ({
      ...previous,
      status: 'unauthenticated',
      error: undefined,
      session: { authenticated: false }
    }));
  }, []);

  const refreshSession = useCallback(async () => {
    if (!tenantCode) {
      clearSession();
      return;
    }

    setRefreshNonce(previous => previous + 1);
  }, [clearSession, tenantCode]);

  useEffect(() => {
    let active = true;

    const oauthErrorCode = clearOAuthQueryFlag();
    if (oauthErrorCode && active) {
      setState(previous => ({
        ...previous,
        oauthError: `Falha no login OAuth (${oauthErrorCode}).`
      }));
    }

    if (!tenantCode) {
      clearSession();
      return () => {
        active = false;
      };
    }

    setState(previous => ({
      ...previous,
      status: previous.status === 'authenticated' ? 'authenticated' : 'loading',
      error: undefined
    }));

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
  }, [tenantCode, refreshNonce]);

  const value = useMemo<AuthSessionContextValue>(() => ({
    ...state,
    refreshSession,
    clearSession
  }), [clearSession, refreshSession, state]);
  return <AuthSessionContext.Provider value={value}>{children}</AuthSessionContext.Provider>;
}

export function useAuthSessionContext(): AuthSessionContextValue {
  const context = useContext(AuthSessionContext);
  if (!context) {
    throw new Error('useAuthSessionContext must be used within AuthSessionProvider.');
  }

  return context;
}
