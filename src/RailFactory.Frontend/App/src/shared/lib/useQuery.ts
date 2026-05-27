import { useState, useEffect, useCallback, useRef } from 'react';
import { toUiErrorMessage } from './http';

/**
 * Standard return type for all data-fetching hooks in this codebase.
 */
export interface UseQueryResult<T> {
  /** The fetched data, or `null` while loading or on error. */
  data: T | null;
  /** True while the fetch is in progress. */
  loading: boolean;
  /** Human-readable error message if the last fetch failed, otherwise `null`. */
  error: string | null;
  /** Imperatively trigger a re-fetch. */
  reload: () => void;
}

/**
 * Generic hook that wraps an async data-fetching function with loading/error state management.
 *
 * @param fetcher - The async function to call. Receives an `AbortSignal` for cleanup.
 *   Pass `null` to skip fetching (useful when a required parameter is not yet available).
 * @param deps - Dependency array — the same values you would put in a `useEffect`.
 *   The fetch re-runs whenever any value in this array changes (e.g. `[tenantCode]`).
 *   **Required**: omitting this causes stale data after parameter changes.
 * @param fallbackMessage - Fallback error message shown if the error has no `.message`.
 *
 * @example
 * ```ts
 * const { data, loading, error, reload } = useQuery(
 *   signal => listWorkCenters(tenantCode, signal),
 *   [tenantCode],
 *   'Falha ao carregar centros de trabalho'
 * );
 * ```
 */
export function useQuery<T>(
  fetcher: ((signal: AbortSignal) => Promise<T>) | null,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  deps: readonly unknown[],
  fallbackMessage: string
): UseQueryResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState<boolean>(fetcher !== null);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  const reload = useCallback(() => setRevision(r => r + 1), []);

  useEffect(() => {
    const currentFetcher = fetcherRef.current;
    if (currentFetcher === null) {
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    let cancelled = false;

    setLoading(true);
    setError(null);

    currentFetcher(controller.signal)
      .then(result => {
        if (!cancelled) {
          setData(result);
          setError(null);
        }
      })
      .catch(err => {
        if (!cancelled && !(err instanceof DOMException && err.name === 'AbortError')) {
          setError(toUiErrorMessage(err, fallbackMessage));
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
      controller.abort();
    };
    // deps is passed explicitly by the caller — it encodes what causes a re-fetch.
    // revision is the imperative reload trigger.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [revision, ...deps]);

  return { data, loading, error, reload };
}
