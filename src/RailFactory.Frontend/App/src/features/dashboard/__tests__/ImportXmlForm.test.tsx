import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ImportXmlForm } from '../ImportXmlForm';

describe('ImportXmlForm', () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('uses the single XML endpoint when one XML file is selected', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ receiptId: 'receipt-1' })
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ImportXmlForm tenantCode="dev" />);
    const file = new File(['<receipt />'], 'single.xml', { type: 'application/xml' });
    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files: [file] } });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Import XML' })).toBeInTheDocument());
    fireEvent.submit(screen.getByRole('button', { name: 'Import XML' }).closest('form')!);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/supply-chain/receipts/import/xml',
      expect.objectContaining({
        method: 'POST',
        body: expect.any(FormData)
      })
    );
  });

  it('uses the batch endpoint when multiple XML files are selected', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        imported: [
          { fileName: 'one.xml', receiptId: 'receipt-1', receiptNumber: 'NFE-1', documentNumber: '1' },
          { fileName: 'two.xml', receiptId: 'receipt-2', receiptNumber: 'NFE-2', documentNumber: '2' }
        ]
      })
    });
    vi.stubGlobal('fetch', fetchMock);

    render(<ImportXmlForm tenantCode="dev" />);
    const files = [
      new File(['<receipt />'], 'one.xml', { type: 'application/xml' }),
      new File(['<receipt />'], 'two.xml', { type: 'application/xml' })
    ];

    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files } });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Import 2 files' })).toBeInTheDocument());
    fireEvent.submit(screen.getByRole('button', { name: 'Import 2 files' }).closest('form')!);

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/supply-chain/receipts/import/xml/batch',
      expect.objectContaining({
        method: 'POST',
        body: expect.any(FormData)
      })
    );
  });

  it('shows per-file batch errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      json: async () => ({
        code: 'receipt.batch_invalid',
        errors: [{ fileName: 'bad.xml', message: 'NF-e XML schema validation failed.' }]
      })
    }));

    render(<ImportXmlForm tenantCode="dev" />);
    const files = [
      new File(['<receipt />'], 'ok.xml', { type: 'application/xml' }),
      new File(['<bad />'], 'bad.xml', { type: 'application/xml' })
    ];

    fireEvent.change(document.querySelector('input[type="file"]')!, { target: { files } });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Import 2 files' })).toBeInTheDocument());
    fireEvent.submit(screen.getByRole('button', { name: 'Import 2 files' }).closest('form')!);

    expect(await screen.findByText(/bad.xml: NF-e XML schema validation failed./)).toBeInTheDocument();
  });
});
