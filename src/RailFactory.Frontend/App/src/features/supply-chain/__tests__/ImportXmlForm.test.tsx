import React from 'react';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ImportXmlForm } from '../components/ImportXmlForm';
import { AuthSessionProvider } from '../../auth';

function renderWithAuth(component: React.ReactElement) {
  return render(
    <AuthSessionProvider tenantCode="dev">
      {component}
    </AuthSessionProvider>
  );
}

describe('ImportXmlForm', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('uses the preview and confirm flow for single XML file', async () => {
    const fetchMock = vi.fn()
      .mockImplementation(async (url) => {
        if (url === '/api/iam/auth/session') {
          return {
            status: 200,
            ok: true,
            json: async () => ({ authenticated: true, user: { permissions: ['supplychain.write'] } })
          };
        }
        if (url === '/api/iam/auth/csrf') {
          return { ok: true, json: async () => ({ token: 'mock-csrf' }) };
        }
        if (url === '/api/supply-chain/receipts/import/xml/preview') {
          return {
            ok: true,
            json: async () => ({
              receiptNumber: 'NFE-1',
              documentNumber: '1',
              receiptDate: '2026-05-05',
              supplierFiscalId: '12345678000199',
              supplierName: 'ACME SUPPLIER',
              items: [{ materialCode: 'MAT-001', quantity: 10, unitOfMeasure: 'UN' }]
            })
          };
        }
        if (url === '/api/supply-chain/receipts/import/xml') {
          return {
            ok: true,
            json: async () => ({ receiptId: 'receipt-1' })
          };
        }
        return { ok: false };
      });
    vi.stubGlobal('fetch', fetchMock);

    renderWithAuth(<ImportXmlForm tenantCode="dev" />);
    const file = new File(['<receipt />'], 'single.xml', { type: 'application/xml' });
    
    // Select file
    await waitFor(() => {
      expect(document.querySelector('input[type="file"]')).toBeTruthy();
    });
    const input = document.querySelector('input[type="file"]')!;
    fireEvent.change(input, { target: { files: [file] } });
    
    // Wait for preview to call /preview
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      '/api/supply-chain/receipts/import/xml/preview',
      expect.anything()
    ));

    // Wait for the Confirm button
    const confirmButton = await screen.findByRole('button', { name: /CONFIRMAR E IMPORTAR PARA ESTOQUE/i });
    expect(confirmButton).toBeInTheDocument();
    
    expect(screen.getByText('Fornecedor')).toBeInTheDocument();
    expect(screen.getByText('ACME SUPPLIER')).toBeInTheDocument();

    // Click confirm (no form, just a button)
    fireEvent.click(confirmButton);

    // Verify /import call
    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/supply-chain/receipts/import/xml',
      expect.objectContaining({
        method: 'POST'
      })
    );
    
    expect(await screen.findByText(/XML importado com sucesso/)).toBeInTheDocument();
  });

  it('uses the batch endpoint when multiple XML files are selected', async () => {
    const fetchMock = vi.fn().mockImplementation(async (url) => {
      if (url === '/api/iam/auth/session') {
        return {
          status: 200,
          ok: true,
          json: async () => ({ authenticated: true, user: { permissions: ['supplychain.write'] } })
        };
      }
      if (url === '/api/iam/auth/csrf') {
        return { ok: true, json: async () => ({ token: 'mock-csrf' }) };
      }

      return {
        ok: true,
        json: async () => ({
          imported: [
            { fileName: 'one.xml', receiptId: 'receipt-1', receiptNumber: 'NFE-1', documentNumber: '1' },
            { fileName: 'two.xml', receiptId: 'receipt-2', receiptNumber: 'NFE-2', documentNumber: '2' }
          ]
        })
      };
    });
    vi.stubGlobal('fetch', fetchMock);

    renderWithAuth(<ImportXmlForm tenantCode="dev" />);
    const files = [
      new File(['<receipt />'], 'one.xml', { type: 'application/xml' }),
      new File(['<receipt />'], 'two.xml', { type: 'application/xml' })
    ];

    await waitFor(() => {
      expect(document.querySelector('input[type="file"]')).toBeTruthy();
    });
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files } });
    
    const importButton = await screen.findByRole('button', { name: /Importar 2 arquivos/i });
    fireEvent.click(importButton);

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/supply-chain/receipts/import/xml/batch',
      expect.objectContaining({
        method: 'POST'
      })
    );
  });

  it('shows per-file batch errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation(async (url) => {
      if (url === '/api/iam/auth/session') {
        return {
          status: 200,
          ok: true,
          json: async () => ({ authenticated: true, user: { permissions: ['supplychain.write'] } })
        };
      }
      if (url === '/api/iam/auth/csrf') {
        return { ok: true, json: async () => ({ token: 'mock-csrf' }) };
      }

      return {
        ok: true,
        json: async () => ({
          code: 'receipt.batch_invalid',
          errors: [{ fileName: 'bad.xml', message: 'NF-e XML schema validation failed.' }]
        })
      };
    }));

    renderWithAuth(<ImportXmlForm tenantCode="dev" />);
    const files = [
      new File(['<receipt />'], 'ok.xml', { type: 'application/xml' }),
      new File(['<bad />'], 'bad.xml', { type: 'application/xml' })
    ];

    await waitFor(() => {
      expect(document.querySelector('input[type="file"]')).toBeTruthy();
    });
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files } });
    const importButton = await screen.findByRole('button', { name: /Importar 2 arquivos/i });
    fireEvent.click(importButton);

    expect(await screen.findByText(/NF-e XML schema validation failed/i)).toBeInTheDocument();
  });
});
