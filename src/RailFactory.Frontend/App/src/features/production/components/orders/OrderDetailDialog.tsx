import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  Divider,
  IconButton,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  CheckCircle,
  ClipboardCheck,
  FlaskConical,
  History,
  Play,
  X,
  XCircle,
  Zap,
} from 'lucide-react';
import { Authorized } from '../../../auth';
import { useAuthSessionContext } from '../../../auth/context/AuthSessionContext';
import {
  releaseProductionOrder,
  startOrderExecution,
  cancelProductionOrder,
  completeProductionOrder,
  getOrderExecutionHistory,
  getBom,
  recordInspection,
} from '../../api/production';
import type { ProductionOrder, OrderExecutionHistory, Bom } from '../../types';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import { MaterialExecutionForm } from './MaterialExecutionForm';
import { ExecutionHistory } from './ExecutionHistory';

// ── Header palette ────────────────────────────────────────────────────────────

const HEADER: Record<string, { bg: string; sublabel: string }> = {
  Draft:       { bg: '#1565c0', sublabel: 'Aguardando liberação' },
  Released:    { bg: '#0277bd', sublabel: 'Materiais reservados — pronto para iniciar' },
  InExecution: { bg: '#bf360c', sublabel: 'Produção em andamento' },
  Completed:   { bg: '#1b5e20', sublabel: 'Concluída com sucesso' },
  Cancelled:   { bg: '#424242', sublabel: 'Cancelada' },
};

// ── Status stepper ────────────────────────────────────────────────────────────

const STEPS = [
  { key: 'Draft',       label: 'Rascunho' },
  { key: 'Released',    label: 'Liberada'  },
  { key: 'InExecution', label: 'Execução'  },
  { key: 'Completed',   label: 'Concluída' },
] as const;

function StatusStepper({ statusKey }: { statusKey: string }) {
  const cancelled = statusKey === 'Cancelled';
  const activeIdx = STEPS.findIndex(s => s.key === statusKey);
  const headBg = HEADER[statusKey]?.bg ?? '#555';

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', px: 3, py: 1.25, bgcolor: 'rgba(0,0,0,0.2)' }}>
      {STEPS.map((s, i) => {
        const done   = !cancelled && i < activeIdx;
        const active = !cancelled && i === activeIdx;
        return (
          <React.Fragment key={s.key}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.6, flexShrink: 0 }}>
              <Box sx={{
                width: 20, height: 20, borderRadius: '50%',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                bgcolor: done ? 'rgba(255,255,255,0.9)' : active ? '#fff' : 'rgba(255,255,255,0.2)',
                flexShrink: 0,
              }}>
                {done
                  ? <CheckCircle size={12} color={headBg} />
                  : <Box sx={{ width: 6, height: 6, borderRadius: '50%', bgcolor: active ? headBg : 'rgba(255,255,255,0.5)' }} />}
              </Box>
              <Typography sx={{
                fontSize: '0.65rem', fontWeight: active ? 800 : 400, whiteSpace: 'nowrap',
                color: (cancelled || i > activeIdx) ? 'rgba(255,255,255,0.4)' : '#fff',
              }}>
                {s.label}
              </Typography>
            </Box>
            {i < STEPS.length - 1 && (
              <Box sx={{ flex: 1, height: 1, mx: 0.75, bgcolor: done ? 'rgba(255,255,255,0.6)' : 'rgba(255,255,255,0.15)' }} />
            )}
          </React.Fragment>
        );
      })}
      {cancelled && (
        <>
          <Box sx={{ flex: 1, height: 1, mx: 0.75, bgcolor: 'rgba(255,255,255,0.15)' }} />
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.6 }}>
            <XCircle size={14} color="rgba(255,255,255,0.55)" />
            <Typography sx={{ fontSize: '0.65rem', color: 'rgba(255,255,255,0.55)' }}>Cancelada</Typography>
          </Box>
        </>
      )}
    </Box>
  );
}

// ── Tab panels ────────────────────────────────────────────────────────────────

/** Tab 1 — Inspeção + conclusão em uma única ação */
function ConcludeTab({ tenantCode, orderId, bom, plannedQuantity, onDone }: {
  tenantCode: string;
  orderId: string;
  bom: Bom | null;
  plannedQuantity: number;
  onDone: (inspectionPassed: boolean) => Promise<void>;
}) {
  const { session } = useAuthSessionContext();
  const inspectedBy = session.authenticated
    ? (session.user?.name ?? session.user?.email ?? 'Usuário')
    : 'Usuário';

  const [result, setResult] = useState<'Passed' | 'Failed'>('Passed');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const items = bom?.items ?? [];

  const handleSubmit = async () => {
    setSaving(true);
    setError(null);
    try {
      await recordInspection(tenantCode, orderId, {
        result,
        inspectedBy,
        notes: notes.trim() || undefined,
      });
      await onDone(result === 'Passed');
      if (result === 'Passed') setNotes('');
    } catch (err) {
      setError(toUiErrorMessage(err, 'Operação não concluída.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box>
      {/* BOM summary */}
      {items.length > 0 && (
        <Box sx={{ mb: 3, p: 2, bgcolor: '#f5f5f5', borderRadius: 2 }}>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase', display: 'block', mb: 1 }}>
            Materiais que serão baixados automaticamente
          </Typography>
          <Table size="small" sx={{ '& td, & th': { py: 0.5, border: 0 } }}>
            <TableBody>
              {items.map(item => (
                <TableRow key={item.id}>
                  <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: '0.8rem', pl: 0 }}>{item.materialCode}</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600, fontSize: '0.8rem' }}>
                    {(item.quantity * plannedQuantity).toLocaleString('pt-BR', { maximumFractionDigits: 4 })}
                  </TableCell>
                  <TableCell sx={{ color: 'text.secondary', fontSize: '0.75rem', pr: 0 }}>{item.unitOfMeasure}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block', fontStyle: 'italic' }}>
            Se o consumo real foi diferente, ajuste na aba "Ajustes" antes de concluir.
          </Typography>
        </Box>
      )}

      {/* Inspeção + ação unificada */}
      {error && <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>{error}</Alert>}

      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 1.5 }}>Resultado da Inspeção</Typography>

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        <Button
          fullWidth variant={result === 'Passed' ? 'contained' : 'outlined'} color="success"
          onClick={() => setResult('Passed')}
          startIcon={<CheckCircle size={16} />}
          sx={{ fontWeight: 800, py: 1.25 }}
        >
          Aprovado
        </Button>
        <Button
          fullWidth variant={result === 'Failed' ? 'contained' : 'outlined'} color="error"
          onClick={() => setResult('Failed')}
          startIcon={<XCircle size={16} />}
          sx={{ fontWeight: 800, py: 1.25 }}
        >
          Reprovado
        </Button>
      </Stack>

      <TextField
        label="Observações"
        placeholder={
          result === 'Failed'
            ? 'Descreva o defeito encontrado, localização, medição fora de especificação...'
            : 'Aspectos verificados, conformidade com especificação, número do lote...'
        }
        size="small" fullWidth multiline rows={4}
        value={notes}
        onChange={e => setNotes(e.target.value)}
        sx={{ mb: 1.5 }}
      />

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2.5 }}>
        Inspecionado por: <strong>{inspectedBy}</strong>
      </Typography>

      <Authorized permission="production.write">
        <Button
          fullWidth variant="contained"
          color={result === 'Passed' ? 'success' : 'error'}
          size="large"
          sx={{ fontWeight: 800, py: 1.5 }}
          startIcon={saving ? <CircularProgress size={18} color="inherit" /> : <CheckCircle size={18} />}
          disabled={saving}
          onClick={() => void handleSubmit()}
        >
          {result === 'Passed' ? 'Aprovar e Concluir Ordem' : 'Registrar Reprovação'}
        </Button>
      </Authorized>
    </Box>
  );
}

/** Tab 2 — Ajustes: consumo manual e scrap (opcional) */
function AdjustmentsTab({ tenantCode, orderId, bom, plannedQuantity, onRecorded }: {
  tenantCode: string;
  orderId: string;
  bom: Bom | null;
  plannedQuantity: number;
  onRecorded: () => void;
}) {
  const items = bom?.items ?? [];
  return (
    <Box>
      <Alert severity="info" sx={{ mb: 3 }}>
        Use esta aba se o consumo real foi diferente do planejado, ou para registrar material descartado (scrap).
      </Alert>

      <Box sx={{ mb: 3 }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 0.5 }}>Consumo Manual</Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5 }}>
          Substitui o backflush automático para este material.
        </Typography>
        <MaterialExecutionForm
          tenantCode={tenantCode}
          orderId={orderId}
          mode="consumption"
          bomItems={items}
          plannedQuantity={plannedQuantity}
          onRecorded={onRecorded}
        />
      </Box>

      <Divider sx={{ mb: 3 }} />

      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 0.5 }}>Registrar Scrap</Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5 }}>
          Material descartado durante a execução.
        </Typography>
        <MaterialExecutionForm
          tenantCode={tenantCode}
          orderId={orderId}
          mode="scrap"
          bomItems={items}
          plannedQuantity={plannedQuantity}
          onRecorded={onRecorded}
        />
      </Box>
    </Box>
  );
}

/** Tab 3 — Histórico */
function HistoryTab({ history, loading, error, onRefresh }: {
  history: OrderExecutionHistory | null;
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}) {
  return (
    <ExecutionHistory history={history} loading={loading} error={error} onRefresh={onRefresh} />
  );
}

// ── Main dialog ───────────────────────────────────────────────────────────────

export function OrderDetailDialog({ open, order, workCenterName, tenantCode, onTransition, onClose }: {
  open: boolean;
  order: ProductionOrder;
  workCenterName: string;
  tenantCode: string;
  onTransition: (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => Promise<void>;
  onClose: () => void;
}) {
  const [tab, setTab] = useState(0);
  const [transitioning, setTransitioning] = useState(false);
  const [transitionError, setTransitionError] = useState<string | null>(null);
  const [bom, setBom] = useState<Bom | null>(null);
  const [history, setHistory] = useState<OrderExecutionHistory | null>(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);

  useEffect(() => {
    setTab(0);
    setTransitionError(null);
    setBom(null);
    setHistory(null);
  }, [order.id]);

  // Load BOM when dialog opens
  useEffect(() => {
    if (open) getBom(tenantCode, order.bomId).then(setBom).catch(() => setBom(null));
  }, [open, order.bomId]);

  // Load history when history tab is selected
  useEffect(() => {
    if (tab === 2 && order.status.key === 'InExecution' && history === null && !historyLoading) {
      void loadHistory();
    }
  }, [tab]);

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

  const doTransition = async (action: () => Promise<void>, newStatus: ProductionOrder['status']) => {
    setTransitioning(true);
    setTransitionError(null);
    try {
      await onTransition(action, order.id, newStatus);
    } catch (err) {
      setTransitionError(toUiErrorMessage(err, 'Operação não concluída.'));
    } finally {
      setTransitioning(false);
    }
  };

  const statusKey = order.status.key;
  const inExecution = statusKey === 'InExecution';
  const header = HEADER[statusKey] ?? { bg: '#555', sublabel: '' };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth={inExecution ? 'md' : 'sm'}
      fullWidth
      slotProps={{ paper: { sx: { borderRadius: 3, overflow: 'hidden' } } }}
    >
      {/* ── Colored header ── */}
      <Box sx={{ bgcolor: header.bg, color: '#fff' }}>
        <Box sx={{ px: 3, pt: 2.5, pb: 1.5, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            <Typography sx={{ fontSize: '0.68rem', fontWeight: 700, opacity: 0.7, textTransform: 'uppercase', letterSpacing: 0.8 }}>
              Ordem de Produção
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 800, mt: 0.25, letterSpacing: -0.5 }}>
              {order.orderNumber}
            </Typography>
            <Typography sx={{ fontSize: '0.85rem', mt: 0.5, opacity: 0.9 }}>
              <strong>{order.productCode}</strong>
              {' · '}{workCenterName}
              {' · '}Qtd: <strong>{order.plannedQuantity.toLocaleString('pt-BR')}</strong>
            </Typography>
            <Typography sx={{ fontSize: '0.72rem', mt: 0.25, opacity: 0.65, fontStyle: 'italic' }}>
              {header.sublabel}
            </Typography>
          </Box>
          <Tooltip title="Fechar">
            <IconButton onClick={onClose} sx={{ color: 'rgba(255,255,255,0.75)', mt: -0.5, mr: -1 }}>
              <X size={20} />
            </IconButton>
          </Tooltip>
        </Box>
        <StatusStepper statusKey={statusKey} />

        {/* Tabs — only for InExecution */}
        {inExecution && (
          <Tabs
            value={tab}
            onChange={(_, v) => setTab(v as number)}
            sx={{
              px: 2,
              '& .MuiTab-root': { color: 'rgba(255,255,255,0.6)', minHeight: 44, fontSize: '0.8rem', fontWeight: 600 },
              '& .Mui-selected': { color: '#fff !important', fontWeight: 800 },
              '& .MuiTabs-indicator': { bgcolor: '#fff', height: 3 },
            }}
          >
            <Tab icon={<ClipboardCheck size={15} />} iconPosition="start" label="Concluir" />
            <Tab icon={<FlaskConical size={15} />} iconPosition="start" label="Ajustes" />
            <Tab icon={<History size={15} />} iconPosition="start" label="Histórico" />
          </Tabs>
        )}
      </Box>

      {/* ── Body ── */}
      <DialogContent sx={{ p: 3, maxHeight: inExecution ? '65vh' : undefined, overflowY: 'auto' }}>

        {transitionError && (
          <Alert severity="error" onClose={() => setTransitionError(null)} sx={{ mb: 2 }}>
            {transitionError}
          </Alert>
        )}

        {/* InExecution — tab panels */}
        {inExecution && tab === 0 && (
          <ConcludeTab
            tenantCode={tenantCode}
            orderId={order.id}
            bom={bom}
            plannedQuantity={order.plannedQuantity}
            onDone={async (passed) => {
              setHistory(null);
              if (passed) {
                await doTransition(
                  () => completeProductionOrder(tenantCode, order.id),
                  { key: 'Completed', label: 'Concluída', color: 'success' },
                );
              }
            }}
          />
        )}
        {inExecution && tab === 1 && (
          <AdjustmentsTab
            tenantCode={tenantCode}
            orderId={order.id}
            bom={bom}
            plannedQuantity={order.plannedQuantity}
            onRecorded={() => setHistory(null)}
          />
        )}
        {inExecution && tab === 2 && (
          <HistoryTab
            history={history}
            loading={historyLoading}
            error={historyError}
            onRefresh={() => void loadHistory()}
          />
        )}

        {/* Draft */}
        {statusKey === 'Draft' && (
          <Authorized permission="production.write">
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Verifique o produto, a BOM e o centro de trabalho. Ao liberar, os materiais serão reservados no estoque e a execução ficará autorizada.
            </Typography>
            <Button fullWidth variant="contained" color="primary" size="large"
              sx={{ fontWeight: 800, py: 1.25, mb: 1.5 }}
              startIcon={transitioning ? <CircularProgress size={18} color="inherit" /> : <Zap size={18} />}
              disabled={transitioning}
              onClick={() => void doTransition(() => releaseProductionOrder(tenantCode, order.id), { key: 'Released', label: 'Liberada', color: 'info' })}>
              Liberar Ordem
            </Button>
            <Button fullWidth variant="outlined" color="error"
              disabled={transitioning}
              onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), { key: 'Cancelled', label: 'Cancelada', color: 'error' })}>
              Cancelar Ordem
            </Button>
          </Authorized>
        )}

        {/* Released */}
        {statusKey === 'Released' && (
          <Authorized permission="production.write">
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              Materiais reservados. Confirme que o operador está no posto e inicie a execução na linha de produção.
            </Typography>
            <Button fullWidth variant="contained" color="success" size="large"
              sx={{ fontWeight: 800, py: 1.25, mb: 1.5 }}
              startIcon={transitioning ? <CircularProgress size={18} color="inherit" /> : <Play size={18} />}
              disabled={transitioning}
              onClick={() => void doTransition(() => startOrderExecution(tenantCode, order.id), { key: 'InExecution', label: 'Em Execução', color: 'warning' })}>
              Iniciar Execução
            </Button>
            <Button fullWidth variant="outlined" color="error"
              disabled={transitioning}
              onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), { key: 'Cancelled', label: 'Cancelada', color: 'error' })}>
              Cancelar Ordem
            </Button>
          </Authorized>
        )}

        {/* Completed / Cancelled */}
        {(statusKey === 'Completed' || statusKey === 'Cancelled') && (
          <>
            <Alert severity={statusKey === 'Completed' ? 'success' : 'error'} sx={{ mb: 2 }}>
              {statusKey === 'Completed' ? 'Ordem concluída com sucesso.' : 'Esta ordem foi cancelada.'}
            </Alert>
            <ExecutionHistory
              history={history}
              loading={historyLoading}
              error={historyError}
              onRefresh={() => void loadHistory()}
            />
            {history === null && !historyLoading && (
              <Button size="small" variant="text" onClick={() => void loadHistory()} sx={{ mt: 1 }}>
                Ver histórico de registros
              </Button>
            )}
          </>
        )}

      </DialogContent>
    </Dialog>
  );
}
