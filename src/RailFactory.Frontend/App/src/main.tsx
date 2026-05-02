import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Boxes, CheckCircle2, Lock, Server } from 'lucide-react';
import './styles.css';

type Status = {
  service: string;
  environment: string;
  tenant: {
    code: string;
  };
  gateway: unknown;
};

function clearOAuthQueryFlag() {
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

type SessionState = {
  authenticated: boolean;
  user?: {
    name?: string;
    email?: string;
  };
};

function App() {
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [session, setSession] = useState<SessionState | null>(null);
  const [sessionLoaded, setSessionLoaded] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const tenantCode = import.meta.env.VITE_TENANT_CODE ?? 'dev';
  const routePath = window.location.pathname;
  const isProtectedRoute = routePath.startsWith('/app');
  const loginHref = `/api/auth/google/start?tenantCode=${encodeURIComponent(tenantCode)}&returnUrl=${encodeURIComponent('/app')}`;

  useEffect(() => {
    const oauthError = clearOAuthQueryFlag();
    if (oauthError) {
      setAuthError(`Falha no login OAuth (${oauthError}).`);
    }

    fetch('/api/status', {
      credentials: 'include',
      headers: {
        'X-Tenant-Code': tenantCode
      }
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Status request failed: ${response.status}`);
        }

        return response.json() as Promise<Status>;
      })
      .then(setStatus)
      .catch((requestError: Error) => setError(requestError.message));

    fetch('/api/auth/session', {
      credentials: 'include',
      headers: {
        'X-Tenant-Code': tenantCode
      }
    })
      .then(async response => {
        if (response.status === 401) {
          try {
            const unauthorizedPayload = (await response.json()) as SessionState;
            setSession(unauthorizedPayload);
          } catch {
            setSession({ authenticated: false });
          }
          return;
        }

        if (!response.ok) {
          throw new Error(`Session request failed: ${response.status}`);
        }

        const payload = (await response.json()) as SessionState;
        setSession(payload);
      })
      .catch((requestError: Error) => {
        setError(requestError.message);
        setSession({ authenticated: false });
      })
      .finally(() => setSessionLoaded(true));
  }, [tenantCode]);

  if (isProtectedRoute && !sessionLoaded) {
    return (
      <main className="shell">
        <section className="workspace protected">
          <div className="panel">
            <p>Validando sessao...</p>
          </div>
        </section>
      </main>
    );
  }

  if (isProtectedRoute && sessionLoaded && !session?.authenticated) {
    return (
      <main className="shell">
        <section className="workspace protected">
          <div className="panel">
            <div className="panel-title">
              <Lock size={20} aria-hidden="true" />
              <h2>Acesso nao autorizado</h2>
            </div>
            <p>Esta area exige sessao autenticada no servidor.</p>
            <p>
              <a href={loginHref}>Entrar com Google</a>
            </p>
            <p>
              <a href="/" className="link-button">
                Voltar para inicio
              </a>
            </p>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="shell">
      <section className="topbar">
        <div className="brand">
          <Boxes size={24} aria-hidden="true" />
          <div>
            <h1>Rail-Factory Fork</h1>
            <p>Tenant dev</p>
          </div>
        </div>
        <span className="badge">P0</span>
      </section>

      {isProtectedRoute ? (
        <section className="workspace protected">
          <div className="panel">
            <div className="panel-title">
              <Lock size={20} aria-hidden="true" />
            <h2>Area protegida</h2>
            </div>
            <p>Voce acessou esta area apos login com Google.</p>
            <p>Tenant ativo: {tenantCode}</p>
            <p>Usuario: {session?.user?.email ?? session?.user?.name ?? 'autenticado'}</p>
            <p>
              <a href="/" className="link-button">
                Voltar para inicio
              </a>
            </p>
          </div>
        </section>
      ) : (
      <section className="workspace">
        <div className="panel">
          <div className="panel-title">
            <Server size={20} aria-hidden="true" />
            <h2>Base Aspire</h2>
          </div>
          <p>
            Fluxo tenant-aware com BFF, Gateway e microservicos em baseline arquitetural definitivo.
          </p>
          {authError ? <p>{authError}</p> : null}
          <p>
            <a href={loginHref}>Entrar com Google</a>
          </p>
          {session?.authenticated ? (
            <p>
              <a href="/app" className="link-button">
                Ir para area protegida
              </a>
            </p>
          ) : null}
        </div>

        <div className="panel">
          <div className="panel-title">
            <CheckCircle2 size={20} aria-hidden="true" />
            <h2>Status</h2>
          </div>
          {error ? (
            <pre className="status error">{error}</pre>
          ) : status ? (
            <pre className="status">{JSON.stringify(status, null, 2)}</pre>
          ) : (
            <pre className="status">Carregando status...</pre>
          )}
        </div>
      </section>
      )}
    </main>
  );
}

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
