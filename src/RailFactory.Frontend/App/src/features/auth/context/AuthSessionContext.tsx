import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { clearOAuthQueryFlag, fetchSession } from '../api/client';
import type { AuthViewState } from '../types';

type AuthSessionContextValue = AuthViewState;

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
      setState(previous => ({
        ...previous,
        status: 'unauthenticated',
        session: { authenticated: false }
      }));
      return () => {
        active = false;
      };
    }

    setState(previous => ({
      ...previous,
      status: 'loading',
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
  }, [tenantCode]);

  const value = useMemo(() => state, [state]);
  return <AuthSessionContext.Provider value={value}>{children}</AuthSessionContext.Provider>;
}

export function useAuthSessionContext(): AuthSessionContextValue {
  const context = useContext(AuthSessionContext);
  if (!context) {
    throw new Error('useAuthSessionContext must be used within AuthSessionProvider.');
  }

  return context;
}
