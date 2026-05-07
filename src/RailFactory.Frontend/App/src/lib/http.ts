export function buildTenantHeaders(tenantCode: string): HeadersInit {
  return {
    'X-Tenant-Code': tenantCode
  };
}

type ProblemPayload = {
  detail?: string;
  title?: string;
};

/**
 * Safely attempts to extract a human-readable error message from a failed response.
 * Handles cases where the response is not valid JSON (e.g., Gateway or Proxy errors).
 */
export async function readProblemMessage(response: Response, fallbackMessage: string): Promise<string> {
  try {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const problem = await response.json() as ProblemPayload | null;
      return problem?.detail ?? problem?.title ?? fallbackMessage;
    }
    
    // Fallback for non-JSON responses (HTML error pages from Nginx/IIS/Gateway)
    const text = await response.text();
    return text.length > 0 && text.length < 150 ? text : fallbackMessage;
  } catch {
    return fallbackMessage;
  }
}

/**
 * Executes a fetch request and automatically handles JSON parsing and standardized error reporting.
 */
export async function fetchJsonOrThrow<T>(
  input: RequestInfo | URL,
  init: RequestInit,
  fallbackMessage: string
): Promise<T> {
  let response: Response;
  try {
    response = await fetch(input, init);
  } catch (networkError) {
    // Handle DNS failure, offline status, or aborted requests
    throw new Error(`Network connection failed: ${fallbackMessage}`);
  }

  if (!response.ok) {
    const message = await readProblemMessage(response, `${fallbackMessage} (${response.status})`);
    throw new Error(message);
  }

  try {
    return await response.json() as T;
  } catch {
    throw new Error(`Invalid response format from server: ${fallbackMessage}`);
  }
}
