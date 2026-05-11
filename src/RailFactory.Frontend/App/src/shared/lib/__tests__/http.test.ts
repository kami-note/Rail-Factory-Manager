import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchJsonOrThrow } from '../http';

describe('fetchJsonOrThrow', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ success: true })
    }));
  });

  it('automatically adds application/json content-type for POST requests with a string body', async () => {
    await fetchJsonOrThrow('/api/test', {
      method: 'POST',
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
      headers: { 'Content-Type': 'text/plain' },
      body: 'just text'
    }, 'Error');

    expect(fetch).toHaveBeenCalledWith('/api/test', expect.objectContaining({
      headers: expect.objectContaining({
        'Content-Type': 'text/plain'
      })
    }));
  });
});
