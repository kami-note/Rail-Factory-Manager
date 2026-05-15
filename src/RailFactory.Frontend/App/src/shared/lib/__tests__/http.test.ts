import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchJsonOrThrow, HttpRequestError } from '../http';

describe('fetchJsonOrThrow', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation(async (url) => {
        if (url === '/api/iam/auth/csrf') {
          return { ok: true, json: async () => ({ token: 'csrf-token-dev' }) };
        }

        return {
          ok: true,
          json: async () => ({ success: true })
        };
      })
    );
  });

  it('automatically adds application/json content-type for POST requests with a string body', async () => {
    await fetchJsonOrThrow('/api/test', {
      method: 'POST',
      headers: { 'X-Tenant-Code': 'dev' },
      body: JSON.stringify({ foo: 'bar' })
    }, 'Error');

    expect(fetch).toHaveBeenCalledWith('/api/test', expect.objectContaining({
      headers: expect.objectContaining({
        'Content-Type': 'application/json'
      })
    }));
  });

  it('does NOT add application/json content-type when body is FormData', async () => {
    const formData = new FormData();
    formData.append('file', new Blob(['test'], { type: 'text/plain' }));

    await fetchJsonOrThrow('/api/test', {
      method: 'POST',
      headers: { 'X-Tenant-Code': 'dev' },
      body: formData
    }, 'Error');

    const lastCall = vi.mocked(fetch).mock.calls[0];
    const init = lastCall[1] as RequestInit;
    const headers = init.headers as Record<string, string> | undefined;

    // It should NOT have Content-Type: application/json
    if (headers) {
      expect(headers['Content-Type']).not.toBe('application/json');
    }
  });

  it('does NOT add application/json content-type when body is URLSearchParams', async () => {
    const params = new URLSearchParams();
    params.append('foo', 'bar');

    await fetchJsonOrThrow('/api/test', {
      method: 'POST',
      headers: { 'X-Tenant-Code': 'dev' },
      body: params
    }, 'Error');

    const lastCall = vi.mocked(fetch).mock.calls[0];
    const init = lastCall[1] as RequestInit;
    const headers = init.headers as Record<string, string> | undefined;

    if (headers) {
      expect(headers['Content-Type']).not.toBe('application/json');
    }
  });

  it('respects existing Content-Type header', async () => {
    await fetchJsonOrThrow('/api/test', {
      method: 'POST',
      headers: {
        'X-Tenant-Code': 'dev',
        'Content-Type': 'text/plain'
      },
      body: 'just text'
    }, 'Error');

    const lastCall = vi.mocked(fetch).mock.calls[0];
    const init = lastCall[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get('Content-Type')).toBe('text/plain');
  });

  it('fails mutation requests without tenant header', async () => {
    await expect(
      fetchJsonOrThrow('/api/test', {
        method: 'POST',
        body: JSON.stringify({ foo: 'bar' })
      }, 'Error')
    ).rejects.toThrow('Missing tenant header for mutation request.');
  });

  it('maps 401 errors to a stable session-expired message', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        clone() {
          return this;
        },
        json: async () => ({ code: 'unauthorized', message: 'Authentication is required.' })
      })
    );

    await expect(
      fetchJsonOrThrow('/api/test', {
        headers: { 'X-Tenant-Code': 'dev' },
        credentials: 'include'
      }, 'Falha ao carregar saldos de estoque')
    ).rejects.toEqual(expect.objectContaining<HttpRequestError>({
      message: 'Sua sessão expirou. Entre novamente para continuar.',
      status: 401,
      code: 'unauthorized'
    }));
  });

  it('maps 403 errors to a stable forbidden message', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 403,
        headers: new Headers({ 'content-type': 'application/json' }),
        clone() {
          return this;
        },
        json: async () => ({ code: 'forbidden', message: 'You do not have permission to access this resource.' })
      })
    );

    await expect(
      fetchJsonOrThrow('/api/test', {
        headers: { 'X-Tenant-Code': 'dev' },
        credentials: 'include'
      }, 'Erro ao carregar permissões')
    ).rejects.toEqual(expect.objectContaining<HttpRequestError>({
      message: 'Você não tem permissão para acessar este recurso.',
      status: 403,
      code: 'forbidden'
    }));
  });

  it('maps tenant mismatch to a stable organization message', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        status: 403,
        headers: new Headers({ 'content-type': 'application/json' }),
        clone() {
          return this;
        },
        json: async () => ({ code: 'tenant.mismatch', message: 'The authenticated internal token does not match the resolved tenant.' })
      })
    );

    await expect(
      fetchJsonOrThrow('/api/test', {
        headers: { 'X-Tenant-Code': 'acme' },
        credentials: 'include'
      }, 'Falha ao carregar dados')
    ).rejects.toEqual(expect.objectContaining<HttpRequestError>({
      message: 'Sua sessão não corresponde à organização selecionada. Entre novamente.',
      status: 403,
      code: 'tenant.mismatch'
    }));
  });
});
