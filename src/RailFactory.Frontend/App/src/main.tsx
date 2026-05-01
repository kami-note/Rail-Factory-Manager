import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Boxes, CheckCircle2, Server } from 'lucide-react';
import './styles.css';

type Status = {
  service: string;
  environment: string;
  tenant: string;
  gateway: unknown;
};

function App() {
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const origin = import.meta.env.VITE_BFF_ORIGIN ?? '';

    fetch(`${origin}/api/status`, {
      credentials: 'include'
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Status request failed: ${response.status}`);
        }

        return response.json() as Promise<Status>;
      })
      .then(setStatus)
      .catch((requestError: Error) => setError(requestError.message));
  }, []);

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

      <section className="workspace">
        <div className="panel">
          <div className="panel-title">
            <Server size={20} aria-hidden="true" />
            <h2>Base Aspire</h2>
          </div>
          <p>
            Scaffold inicial com BFF, Gateway e APIs placeholder para Tenancy, IAM,
            Supply Chain, Inventory e Production.
          </p>
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
    </main>
  );
}

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
