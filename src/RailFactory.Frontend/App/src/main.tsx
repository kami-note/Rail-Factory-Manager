import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { LogOut, ShieldCheck } from 'lucide-react';
import { buildLoginHref, logout, useAuthSession } from './auth';
import './styles.css';

type Status = {
  service: string;
  environment: string;
  tenant: {
    code: string;
  };
  gateway: unknown;
};

function App() {
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState<string | null>(null);
  const tenantCode = import.meta.env.VITE_TENANT_CODE ?? 'dev';
  const routePath = window.location.pathname;
  const isProtectedRoute = routePath.startsWith('/app');
  const loginHref = buildLoginHref(tenantCode, '/app');
  const auth = useAuthSession(tenantCode);
  const [logoutError, setLogoutError] = useState<string | null>(null);

  useEffect(() => {
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
  }, [tenantCode]);

  if (isProtectedRoute && auth.status === 'loading') {
    return (
      <main className="page">
        <section className="card">
          <div className="brand-mark" aria-hidden="true" />
          <p className="brand-name">Rail Factory</p>
          <h1 className="title">Validando sessao</h1>
          <p className="subtitle">Aguarde enquanto confirmamos seu acesso.</p>
        </section>
      </main>
    );
  }

  if (isProtectedRoute && auth.status !== 'authenticated') {
    return (
      <main className="page">
        <section className="stack">
          <header className="hero">
            <div className="brand-mark" aria-hidden="true" />
            <p className="brand-name">Rail Factory</p>
          </header>
          <article className="card auth-card">
            <h1 className="title">Sessao expirada</h1>
            <p className="subtitle">Esta area exige autenticacao ativa no servidor.</p>
            <a href={loginHref} className="primary-button">
              <img src="/google-g-logo.png" alt="" className="google-logo" aria-hidden="true" />
              Sign in with Google
            </a>
            <a href="/" className="text-link">Voltar para inicio</a>
          </article>
        </section>
      </main>
    );
  }

  return (
    <main className="page">
      {isProtectedRoute ? (
        <section className="stack">
          <header className="hero">
            <div className="brand-mark" aria-hidden="true" />
            <p className="brand-name">Rail Factory</p>
          </header>
          <article className="card auth-card">
            <h1 className="title">Area protegida</h1>
            <p className="subtitle">
              <ShieldCheck size={16} aria-hidden="true" /> Sessao ativa para tenant <strong>{tenantCode}</strong>.
            </p>
            <p className="meta-text">Usuario: {auth.session.user?.email ?? auth.session.user?.name ?? 'autenticado'}</p>
            {logoutError ? <p className="error-text">{logoutError}</p> : null}
            <button
              type="button"
              className="primary-button"
              onClick={async () => {
                try {
                  setLogoutError(null);
                  await logout(tenantCode);
                  window.location.href = '/';
                } catch (requestError) {
                  setLogoutError(requestError instanceof Error ? requestError.message : 'Falha ao encerrar sessao.');
                }
              }}
            >
              <LogOut size={16} aria-hidden="true" /> Encerrar sessao
            </button>
            <a href="/" className="text-link">Voltar para inicio</a>
          </article>
          <p className="status-pill">
            <span className="status-dot" aria-hidden="true" />
            SYSTEM STATUS: OPERATIONAL
          </p>
        </section>
      ) : (
        <section className="stack">
          <header className="hero">
            <div className="brand-mark" aria-hidden="true" />
            <p className="brand-name">Rail Factory</p>
          </header>
          <article className="card auth-card">
            <h1 className="title">Sign In</h1>
            <p className="subtitle">Access your industrial dashboard</p>
            <a href={loginHref} className="primary-button">
              <img src="/google-g-logo.png" alt="" className="google-logo" aria-hidden="true" />
              Sign in with Google
            </a>
            <p className="meta-text">
              By continuing, you agree to our <a className="text-link inline" href="#">Terms of Service</a>
            </p>
            {auth.oauthError ? <p className="error-text">{auth.oauthError}</p> : null}
            {auth.status === 'error' && auth.error ? <p className="error-text">{auth.error}</p> : null}
          </article>
          <p className="status-pill">
            <span className="status-dot" aria-hidden="true" />
            SYSTEM STATUS: OPERATIONAL
          </p>
          {status ? <pre className="status-dump">{JSON.stringify(status, null, 2)}</pre> : null}
          {error ? <pre className="status-dump error">{error}</pre> : null}
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
