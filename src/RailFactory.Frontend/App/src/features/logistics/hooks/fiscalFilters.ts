export type FiscalFilterKey = 'all' | 'processing' | 'authorized' | 'error' | 'cancelled';

export const FILTER_STATUSES: Record<string, string[]> = {
  all: [],
  processing: ['processando', 'processando_autorizacao'],
  authorized: ['autorizado', 'CONCLUIDO'],
  error: ['erro_autorizacao', 'REJEITADO', 'denegado', 'DENEGADO'],
  cancelled: ['cancelado', 'CANCELADO'],
};

export const FILTER_LABELS: Record<FiscalFilterKey, string> = {
  all: 'Todas',
  processing: 'Processando',
  authorized: 'Autorizadas',
  error: 'Com Erro',
  cancelled: 'Canceladas',
};

export const FILTER_COLORS: Record<FiscalFilterKey, 'default' | 'info' | 'success' | 'error'> = {
  all: 'default',
  processing: 'info',
  authorized: 'success',
  error: 'error',
  cancelled: 'default',
};
