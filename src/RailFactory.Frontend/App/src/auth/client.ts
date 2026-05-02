import type { AuthSession } from './types';

export function clearOAuthQueryFlag() {
  const query = new URLSearchParams(window.location.search);
  const oauthState = query.get('oauth');
  if (oauthState !== 'success' && oauthState !== 'error') return null;

  const oauthError = oauthState === 'error' ? query.get('error') : null;

  query.delete('oauth');
  query.delete('error');
  const queryString = query.toString();
  const cleanUrl = `${window.location.pathname}${queryString ? `?${queryString}` : ''}${window.location.hash}`;
  window.history.replaceState(null, '', cleanUrl);
  return oauthError;
}

export async function fetchSession(tenantCode: string): Promise<{ statusCode: number; payload: AuthSession }> {
  const response = await fetch('/api/auth/session', {
    credentials: 'include',
    headers: {
      'X-Tenant-Code': tenantCode
    }
  });

  if (response.status === 401) {
    try {
      const unauthorizedPayload = (await response.json()) as AuthSession;
      return { statusCode: 401, payload: unauthorizedPayload };
    } catch {
      return { statusCode: 401, payload: { authenticated: false } };
    }
  }

  if (!response.ok) {
    throw new Error(`Session request failed: ${response.status}`);
  }

  const payload = (await response.json()) as AuthSession;
  return { statusCode: response.status, payload };
}

export function buildLoginHref(tenantCode: string, returnUrl = '/app') {
  return `/api/auth/google/start?tenantCode=${encodeURIComponent(tenantCode)}&returnUrl=${encodeURIComponent(returnUrl)}`;
}

export async function logout(tenantCode: string): Promise<void> {
  const csrfResponse = await fetch('/api/auth/csrf', {
    credentials: 'include',
    headers: {
      'X-Tenant-Code': tenantCode
    }
  });

  if (!csrfResponse.ok) {
    throw new Error(`CSRF request failed: ${csrfResponse.status}`);
  }

  const csrfPayload = (await csrfResponse.json()) as { token?: string };
  if (!csrfPayload.token) {
    throw new Error('Missing CSRF token.');
  }

  const logoutResponse = await fetch('/api/auth/logout', {
    method: 'POST',
    credentials: 'include',
    headers: {
      'X-Tenant-Code': tenantCode,
      'X-CSRF-TOKEN': csrfPayload.token
    }
  });

  if (!logoutResponse.ok && logoutResponse.status !== 401) {
    throw new Error(`Logout request failed: ${logoutResponse.status}`);
  }
}
