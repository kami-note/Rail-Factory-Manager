export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated' | 'error';

export type SessionUser = {
  name?: string;
  email?: string;
  permissions: string[];
};

export type AuthSession = {
  authenticated: boolean;
  user?: SessionUser;
};

export type AuthViewState = {
  status: AuthStatus;
  session: AuthSession;
  error?: string;
  oauthError?: string;
};
