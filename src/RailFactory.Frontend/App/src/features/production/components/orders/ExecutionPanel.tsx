import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import {
  CheckCircle,
  History,
  Play,
  XCircle,
  Zap,
} from 'lucide-react';
import { StatusChip } from '../../../../shared/components/common/StatusChip';
import { Authorized } from '../../../auth';
import {
  releaseProductionOrder,
  startOrderExecution,
  cancelProductionOrder,
  completeProductionOrder,
  getOrderExecutionHistory,
} from '../../api/production';
import type { ProductionOrder, OrderExecutionHistory } from '../../types';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import { ExecutionForms } from './ExecutionForms';
import { ExecutionHistory } from './ExecutionHistory';

export function ExecutionPanel({ tenantCode, order, onTransition }: {
  tenantCode: string;
  order: ProductionOrder;
  onTransition: (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => Promise<void>;
}) {
  // 0 = Ações, 1 = Registros (only InExecution), 2 or 1 = Histórico
  const [tab, setTab] = useState(0);
  const [transitioning, setTransitioning] = useState(false);
  const [history, setHistory] = useState<OrderExecutionHistory | null>(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);

  const loadHistory = async () => {
    setHistoryLoading(true);
    setHistoryError(null);
    try {
      setHistory(await getOrderExecutionHistory(tenantCode, order.id));
    } catch (err) {
      setHistoryError(toUiErrorMessage(err, 'Não foi possível carregar o histórico.'));
    } finally {
      setHistoryLoading(false);
    }
  };

  useEffect(() => {
    if (tab === 2) void loadHistory();
  }, [tab]);

  const doTransition = async (action: () => Promise<void>, newStatus: ProductionOrder['status']) => {
    setTransitioning(true);
    try {
      await onTransition(action, order.id, newStatus);
    } finally {
      setTransitioning(false);
    }
  };

  const tabs = [
    { label: 'Ações' },
    ...(order.status.key === 'InExecution' ? [{ label: 'Registrar' }] : []),
    { label: 'Histórico', icon: <History size={13} /> }
  ];

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>ORDEM DE PRODUÇÃO</Typography>
      <Typography variant="h6" sx={{ fontWeight: 800, mt: 0.5 }}>{order.orderNumber}</Typography>
      <Stack direction="row" spacing={1} sx={{ mt: 1, mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <Chip size="small" label={order.productCode} variant="outlined" sx={{ fontFamily: 'monospace', fontWeight: 700 }} />
        <StatusChip status={order.status} />
      </Stack>

      <Tabs
        value={tab}
        onChange={(_, v) => setTab(v as number)}
        variant="fullWidth"
        sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}
      >
        {tabs.map((t, i) => (
          <Tab key={i} label={t.label} icon={t.icon} iconPosition="start" sx={{ minHeight: 40, fontSize: '0.75rem' }} />
        ))}
      </Tabs>

      {/* Tab: Ações */}
      {tab === 0 && (
        <Authorized permission="production.write">
          <Stack spacing={1}>
            {order.status.key === 'Draft' && (
              <Button fullWidth variant="contained" color="primary"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Zap size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => releaseProductionOrder(tenantCode, order.id), { key: 'Released', label: 'Liberada', color: 'info' })}
                sx={{ fontWeight: 800 }}>
                Liberar Ordem
              </Button>
            )}
            {order.status.key === 'Released' && (
              <Button fullWidth variant="contained" color="success"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Play size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => startOrderExecution(tenantCode, order.id), { key: 'InExecution', label: 'Em Execução', color: 'warning' })}
                sx={{ fontWeight: 800 }}>
                Iniciar Execução
              </Button>
            )}
            {order.status.key === 'InExecution' && (
              <Button fullWidth variant="contained" color="success"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <CheckCircle size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => completeProductionOrder(tenantCode, order.id), { key: 'Completed', label: 'Concluída', color: 'success' })}
                sx={{ fontWeight: 800 }}>
                Concluir Ordem
              </Button>
            )}
            {(order.status.key === 'Draft' || order.status.key === 'Released') && (
              <Button fullWidth variant="outlined" color="error"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <XCircle size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), { key: 'Cancelled', label: 'Cancelada', color: 'error' })}>
                Cancelar Ordem
              </Button>
            )}
            {(order.status.key === 'Completed' || order.status.key === 'Cancelled') && (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                Esta ordem está encerrada.
              </Typography>
            )}
          </Stack>
        </Authorized>
      )}

      {/* Tab: Registrar (only InExecution) */}
      {tab === 1 && order.status.key === 'InExecution' && (
        <ExecutionForms tenantCode={tenantCode} orderId={order.id} onRecorded={() => void loadHistory()} />
      )}

      {/* Tab: Histórico */}
      {tab === (order.status.key === 'InExecution' ? 2 : 1) && (
        <ExecutionHistory
          history={history}
          loading={historyLoading}
          error={historyError}
          onRefresh={() => void loadHistory()}
        />
      )}
    </Box>
  );
}
