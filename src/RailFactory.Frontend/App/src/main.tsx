import React, { useEffect, useMemo, useState } from 'react';
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

type ReceiptItem = {
  id: string;
  materialCode: string;
  expectedQuantity: number;
  unitOfMeasure: string;
};

type Receipt = {
  id: string;
  receiptNumber: string;
  documentNumber: string;
  receiptDate: string;
  status: string;
  createdAt: string;
  itemCount: number;
  items: ReceiptItem[];
};

type Supplier = {
  id: string;
  fiscalId: string;
  name: string;
};

function App() {
  const [status, setStatus] = useState<Status | null>(null);
  const [error, setError] = useState<string | null>(null);
  const tenantCode = import.meta.env.VITE_TENANT_CODE ?? 'dev';
  const routePath = window.location.pathname;
  const isProtectedRoute = routePath.startsWith('/app');
  const isReceiptsRoute = routePath.startsWith('/app/receipts');
  const isNewReceiptRoute = routePath.startsWith('/app/new-receipt');
  const isImportRoute = routePath.startsWith('/app/import-xml');
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
            <nav className="meta-text" style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
              <a className="text-link inline" href="/app/receipts">Receipts</a>
              <a className="text-link inline" href="/app/new-receipt">New Receipt</a>
              <a className="text-link inline" href="/app/import-xml">Import XML</a>
            </nav>
            {isReceiptsRoute ? <ReceiptsList tenantCode={tenantCode} /> : null}
            {isNewReceiptRoute ? <NewReceiptForm tenantCode={tenantCode} /> : null}
            {isImportRoute ? <ImportXmlForm tenantCode={tenantCode} /> : null}
            {!isReceiptsRoute && !isNewReceiptRoute && !isImportRoute ? <p className="meta-text">Escolha um fluxo de P2 no menu acima.</p> : null}
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

function ReceiptsList({ tenantCode }: { tenantCode: string }) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/supply-chain/receipts', {
      headers: {
        'X-Tenant-Code': tenantCode
      },
      credentials: 'include'
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Receipts request failed: ${response.status}`);
        }

        return response.json() as Promise<Receipt[]>;
      })
      .then(data => {
        setReceipts(data);
        setLoading(false);
      })
      .catch((requestError: Error) => {
        setError(requestError.message);
        setLoading(false);
      });
  }, [tenantCode]);

  if (loading) return <p className="meta-text">Loading receipts...</p>;
  if (error) return <p className="error-text">{error}</p>;
  if (receipts.length === 0) return <p className="meta-text">No receipts yet.</p>;

  return (
    <div className="status-dump">
      {receipts.map(receipt => (
        <div key={receipt.id} style={{ marginBottom: 12 }}>
          <strong>{receipt.receiptNumber}</strong> - {receipt.documentNumber} - {receipt.itemCount} item(s)
        </div>
      ))}
    </div>
  );
}

function NewReceiptForm({ tenantCode }: { tenantCode: string }) {
  const [supplierFiscalId, setSupplierFiscalId] = useState('12345678000100');
  const [supplierName, setSupplierName] = useState('Supplier Dev');
  const [receiptNumber, setReceiptNumber] = useState('RCPT-001');
  const [documentNumber, setDocumentNumber] = useState('DOC-001');
  const [materialCode, setMaterialCode] = useState('MAT-001');
  const [quantity, setQuantity] = useState('10');
  const [uom, setUom] = useState('UN');
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  return (
    <form
      style={{ display: 'grid', gap: 8 }}
      onSubmit={async event => {
        event.preventDefault();
        setError(null);
        setResult(null);

        try {
          const supplierResponse = await fetch('/api/supply-chain/suppliers', {
            method: 'POST',
            credentials: 'include',
            headers: {
              'Content-Type': 'application/json',
              'X-Tenant-Code': tenantCode
            },
            body: JSON.stringify({ fiscalId: supplierFiscalId, name: supplierName })
          });

          if (!supplierResponse.ok) {
            throw new Error(`Supplier creation failed: ${supplierResponse.status}`);
          }

          const supplier = (await supplierResponse.json()) as Supplier;

          const receiptResponse = await fetch('/api/supply-chain/receipts', {
            method: 'POST',
            credentials: 'include',
            headers: {
              'Content-Type': 'application/json',
              'X-Tenant-Code': tenantCode
            },
            body: JSON.stringify({
              receiptNumber,
              supplierId: supplier.id,
              documentNumber,
              receiptDate: new Date().toISOString().slice(0, 10),
              items: [
                {
                  materialCode,
                  expectedQuantity: Number(quantity),
                  unitOfMeasure: uom
                }
              ]
            })
          });

          if (!receiptResponse.ok) {
            throw new Error(`Receipt creation failed: ${receiptResponse.status}`);
          }

          const created = await receiptResponse.json();
          setResult(`Receipt created: ${created.id ?? created.receiptId}`);
        } catch (requestError) {
          setError(requestError instanceof Error ? requestError.message : 'Unexpected error creating receipt.');
        }
      }}
    >
      <input value={supplierFiscalId} onChange={e => setSupplierFiscalId(e.target.value)} placeholder="Supplier fiscal id" />
      <input value={supplierName} onChange={e => setSupplierName(e.target.value)} placeholder="Supplier name" />
      <input value={receiptNumber} onChange={e => setReceiptNumber(e.target.value)} placeholder="Receipt number" />
      <input value={documentNumber} onChange={e => setDocumentNumber(e.target.value)} placeholder="Document number" />
      <input value={materialCode} onChange={e => setMaterialCode(e.target.value)} placeholder="Material code" />
      <input value={quantity} onChange={e => setQuantity(e.target.value)} placeholder="Quantity" />
      <input value={uom} onChange={e => setUom(e.target.value)} placeholder="UoM" />
      <button type="submit" className="primary-button">Create receipt</button>
      {result ? <p className="meta-text">{result}</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
    </form>
  );
}

function ImportXmlForm({ tenantCode }: { tenantCode: string }) {
  const sampleXml = useMemo(
    () => `<receipt>\n  <receiptNumber>RCPT-XML-001</receiptNumber>\n  <documentNumber>DOC-XML-001</documentNumber>\n  <receiptDate>${new Date().toISOString().slice(0, 10)}</receiptDate>\n  <supplier>\n    <fiscalId>99887766000100</fiscalId>\n    <name>XML Supplier</name>\n  </supplier>\n  <items>\n    <item>\n      <materialCode>MAT-XML-001</materialCode>\n      <quantity>5</quantity>\n      <uom>UN</uom>\n    </item>\n  </items>\n</receipt>`,
    []
  );

  const [xml, setXml] = useState(sampleXml);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  return (
    <form
      style={{ display: 'grid', gap: 8 }}
      onSubmit={async event => {
        event.preventDefault();
        setResult(null);
        setError(null);

        try {
          const response = await fetch('/api/supply-chain/receipts/import/xml', {
            method: 'POST',
            credentials: 'include',
            headers: {
              'Content-Type': 'application/json',
              'X-Tenant-Code': tenantCode
            },
            body: JSON.stringify({ xmlContent: xml })
          });

          if (!response.ok) {
            throw new Error(`XML import failed: ${response.status}`);
          }

          const data = await response.json();
          setResult(`XML imported. Receipt: ${data.receiptId ?? data.id}`);
        } catch (requestError) {
          setError(requestError instanceof Error ? requestError.message : 'Unexpected XML import error.');
        }
      }}
    >
      <textarea value={xml} onChange={e => setXml(e.target.value)} rows={14} />
      <button type="submit" className="primary-button">Import XML</button>
      {result ? <p className="meta-text">{result}</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
    </form>
  );
}

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
