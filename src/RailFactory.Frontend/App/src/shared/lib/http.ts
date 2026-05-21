export function buildTenantHeaders(tenantCode: string): HeadersInit {
  return {
    'X-Tenant-Code': tenantCode
  };
}

const csrfTokenByTenant = new Map<string, string>();

/**
 * Fetches a fresh CSRF token from the BFF.
 */
export async function fetchCsrfToken(tenantCode: string): Promise<string> {
  if (!tenantCode) {
    throw new Error('Missing tenant code for CSRF token request.');
  }

  const cached = csrfTokenByTenant.get(tenantCode);
  if (cached) {
    return cached;
  }

  const response = await fetch('/api/iam/auth/csrf', {
    headers: {
      ...buildTenantHeaders(tenantCode)
    },
    credentials: 'include'
  });

  if (!response.ok) {
    throw new Error('Failed to retrieve CSRF token.');
  }

  const { token } = await response.json();
  csrfTokenByTenant.set(tenantCode, token);
  return token;
}

type ProblemPayload = {
  code?: string;
  message?: string;
  detail?: string;
  title?: string;
};

export class HttpRequestError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = 'HttpRequestError';
    this.status = status;
    this.code = code;
  }
}

export function toUiErrorMessage(error: unknown, fallbackMessage: string): string {
  return error instanceof Error && error.message
    ? error.message
    : fallbackMessage;
}

function mapErrorMessage(status: number, fallbackMessage: string, problem?: ProblemPayload | null): string {
  const code = problem?.code;

  switch (code) {
    case 'unauthorized':
      return 'Sua sessão expirou. Entre novamente para continuar.';
    case 'forbidden':
      return 'Você não tem permissão para acessar este recurso.';
    case 'csrf_error':
      return 'Sua sessão precisa ser renovada antes de concluir esta ação.';
    case 'csrf_https_required':
      return 'A operação exige conexão HTTPS válida.';
    case 'tenant.code_required':
      return 'Selecione uma organização antes de continuar.';
    case 'tenant.not_found':
      return 'A organização selecionada não foi encontrada.';
    case 'tenant.inactive':
      return 'A organização selecionada está inativa.';
    case 'tenant.mismatch':
      return 'Sua sessão não corresponde à organização selecionada. Entre novamente.';
  }

  switch (status) {
    case 401:
      return 'Sua sessão expirou. Entre novamente para continuar.';
    case 403:
      return 'Você não tem permissão para executar esta ação.';
    case 404:
      return 'O recurso solicitado não foi encontrado.';
    case 409:
      return problem?.message ?? problem?.detail ?? 'A operação entrou em conflito com o estado atual dos dados.';
    case 422:
      return problem?.message ?? problem?.detail ?? 'Os dados informados não puderam ser processados.';
    default:
      return problem?.message ?? problem?.detail ?? problem?.title ?? fallbackMessage;
  }
}

/**
 * Safely attempts to extract a human-readable error message from a failed response.
 * Handles cases where the response is not valid JSON (e.g., Gateway or Proxy errors).
 */
export async function readProblemMessage(response: Response, fallbackMessage: string): Promise<string> {
  try {
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('json')) {
      const problem = await response.json() as ProblemPayload | null;
      return mapErrorMessage(response.status, fallbackMessage, problem);
    }
    
    // Fallback for non-JSON responses (HTML error pages from Nginx/IIS/Gateway)
    const text = await response.text();
    if (text.length > 0 && text.length < 150) {
      return mapErrorMessage(response.status, text, { message: text });
    }

    return mapErrorMessage(response.status, fallbackMessage);
  } catch {
    return mapErrorMessage(response.status, fallbackMessage);
  }
}

/**
 * Executes a fetch request and automatically handles JSON parsing, standardized error reporting,
 * and automatic CSRF token injection for mutation requests.
 */
export async function fetchJsonOrThrow<T>(
  input: RequestInfo | URL,
  init: RequestInit,
  fallbackMessage: string
): Promise<T> {
  // Automatically inject CSRF token for mutation methods
  const method = init.method?.toUpperCase() || 'GET';
  if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(method)) {
    const headers = new Headers(init.headers);
    const tenantCode = headers.get('X-Tenant-Code');

    if (!tenantCode) {
      throw new Error('Missing tenant header for mutation request.');
    }

    let csrfToken = csrfTokenByTenant.get(tenantCode);
    if (!csrfToken) {
      csrfToken = await fetchCsrfToken(tenantCode);
    }

    headers.set('X-CSRF-TOKEN', csrfToken);
    init.headers = headers;
  }

  // Add Content-Type for JSON bodies — use Headers object to avoid losing previously set headers.
  if (typeof init.body === 'string') {
    const ctHeaders = new Headers(init.headers);
    if (!ctHeaders.get('Content-Type')) {
      ctHeaders.set('Content-Type', 'application/json');
      init.headers = ctHeaders;
    }
  }

  let response: Response;
  try {
    response = await fetch(input, init);
  } catch (networkError) {
    // Handle DNS failure, offline status, or aborted requests
    throw new Error(`Network connection failed: ${fallbackMessage}`);
  }

  // On CSRF failure: refresh token and retry once
  if (response.status === 403 && ['POST', 'PUT', 'DELETE', 'PATCH'].includes(method)) {
    const tenantCode = new Headers(init.headers).get('X-Tenant-Code');
    if (tenantCode) {
      csrfTokenByTenant.delete(tenantCode);
      try {
        const freshToken = await fetchCsrfToken(tenantCode);
        const retryHeaders = new Headers(init.headers);
        retryHeaders.set('X-CSRF-TOKEN', freshToken);
        init.headers = retryHeaders;
        response = await fetch(input, init);
      } catch {
        // retry failed — fall through to error handling below
      }
    }
  }

  if (!response.ok) {

    const contentType = response.headers.get('content-type');
    let problemCode: string | undefined;

    if (contentType && contentType.includes('json')) {
      try {
        const clonedProblem = await response.clone().json() as ProblemPayload | null;
        problemCode = clonedProblem?.code;
      } catch {
        problemCode = undefined;
      }
    }

    const message = await readProblemMessage(response, fallbackMessage);
    throw new HttpRequestError(message, response.status, problemCode);
  }

  try {
    return await response.json() as T;
  } catch {
    throw new Error(`Invalid response format from server: ${fallbackMessage}`);
  }
}
