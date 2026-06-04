import React, { useEffect, useState } from 'react';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import {
  CheckCircle,
  ChevronDown,
  ClipboardCheck,
  FlaskConical,
  History,
  Play,
  Trash2,
  X,
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
  getBom,
} from '../../api/production';
import type { ProductionOrder, OrderExecutionHistory, Bom } from '../../types';
import { toUiErrorMessage } from '../../../../shared/lib/http';
import { MaterialExecutionForm } from './MaterialExecutionForm';
import { InspectionForm } from './InspectionForm';
import { ExecutionHistory } from './ExecutionHistory';

// ── Status stepper ────────────────────────────────────────────────────────────

const STEPS: Array<{ key: ProductionOrder['status']['key']; label: string }> = [
  { key: 'Draft',       label: 'Rascunho' },
  { key: 'Released',    label: 'Liberada' },
  { key: 'InExecution', label: 'Execução' },
  { key: 'Completed',   label: 'Concluída' },
];

function StatusStepper({ status }: { status: ProductionOrder['status'] }) {
  const isCancelled = status.key === 'Cancelled';
  const activeIndex = isCancelled ? -1 : STEPS.findIndex(s => s.key === status.key);

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0, my: 2 }}>
      {STEPS.map((step, i) => {
        const done = !isCancelled && i < activeIndex;
        const active = !isCancelled && i === activeIndex;
        const future = isCancelled || i > activeIndex;

        return (
          <React.Fragment key={step.key}>
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 0, flex: 1 }}>
              <Box sx={{
                width: 24, height: 24, borderRadius: '50%',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                bgcolor: done ? 'success.main' : active ? 'primary.main' : '#e0e0e0',
                flexShrink: 0,
              }}>
                {done
                  ? <CheckCircle size={14} color="#fff" />
                  : <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: active ? '#fff' : '#bdbdbd' }} />
                }
              </Box>
              <Typography variant="caption" sx={{
                mt: 0.5, fontSize: '0.62rem', fontWeight: active ? 800 : 500,
                color: done ? 'success.main' : active ? 'primary.main' : 'text.disabled',
                textAlign: 'center', lineHeight: 1.2,
              }}>
                {step.label}
              </Typography>
            </Box>
            {i < STEPS.length - 1 && (
              <Box sx={{ height: 2, flex: 1, bgcolor: done ? 'success.light' : '#e0e0e0', mt: -2.5, mx: 0.25 }} />
            )}
          </React.Fragment>
        );
      })}
      {isCancelled && (
        <Box sx={{ ml: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <Box sx={{ width: 24, height: 24, borderRadius: '50%', bgcolor: 'error.main', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <XCircle size={14} color="#fff" />
          </Box>
          <Typography variant="caption" sx={{ mt: 0.5, fontSize: '0.62rem', fontWeight: 800, color: 'error.main' }}>
            Cancelada
          </Typography>
        </Box>
      )}
    </Box>
  );
}

// ── Next step card ────────────────────────────────────────────────────────────

function NextStepCard({ children, description, color = 'primary' }: {
  children: React.ReactNode;
  description: string;
  color?: 'primary' | 'success' | 'error';
}) {
  const bgMap = { primary: '#e8f0fe', success: '#e8f5e9', error: '#fce4ec' };
  const borderMap = { primary: '#90caf9', success: '#a5d6a7', error: '#f48fb1' };

  return (
    <Box sx={{ border: `1px solid ${borderMap[color]}`, borderRadius: 2, bgcolor: bgMap[color], p: 2, mb: 2 }}>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5, fontStyle: 'italic' }}>
        {description}
      </Typography>
      {children}
    </Box>
  );
}

// ── Collapsible section ───────────────────────────────────────────────────────

function Section({ title, icon, defaultExpanded = false, children }: {
  title: string;
  icon: React.ReactNode;
  defaultExpanded?: boolean;
  children: React.ReactNode;
}) {
  return (
    <Accordion defaultExpanded={defaultExpanded} disableGutters elevation={0}
      sx={{ border: '1px solid #edebe9', borderRadius: '8px !important', '&:before': { display: 'none' }, mb: 1.5 }}>
      <AccordionSummary expandIcon={<ChevronDown size={16} />} sx={{ minHeight: 44, px: 2, py: 0 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          {icon}
          <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
            {title}
          </Typography>
        </Stack>
      </AccordionSummary>
      <AccordionDetails sx={{ pt: 0, px: 2, pb: 2 }}>
        {children}
      </AccordionDetails>
    </Accordion>
  );
}

// ── Main component ────────────────────────────────────────────────────────────

export function ExecutionPanel({ tenantCode, order, workCenterName, onTransition, onClose }: {
  tenantCode: string;
  order: ProductionOrder;
  workCenterName: string;
  onTransition: (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => Promise<void>;
  onClose: () => void;
}) {
  const [transitioning, setTransitioning] = useState(false);
  const [transitionError, setTransitionError] = useState<string | null>(null);
  const [bom, setBom] = useState<Bom | null>(null);
  const [history, setHistory] = useState<OrderExecutionHistory | null>(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);

  useEffect(() => {
    getBom(tenantCode, order.bomId).then(setBom).catch(() => setBom(null));
  }, [order.bomId]);

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

  const isActive = order.status.key !== 'Completed' && order.status.key !== 'Cancelled';

  return (
    <Box sx={{ p: 2.5, display: 'flex', flexDirection: 'column', height: '100%', overflowY: 'auto' }}>

      {/* ── Header ── */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 0.5 }}>
        <Box>
          <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800, lineHeight: 1 }}>
            ORDEM DE PRODUÇÃO
          </Typography>
          <Typography variant="h6" sx={{ fontWeight: 800, mt: 0.25 }}>{order.orderNumber}</Typography>
        </Box>
        <Tooltip title="Fechar">
          <IconButton size="small" onClick={onClose} sx={{ mt: 0.5 }}>
            <X size={16} />
          </IconButton>
        </Tooltip>
      </Box>

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 0.5, mb: 0.5 }}>
        <Chip size="small" label={order.productCode} variant="outlined" sx={{ fontFamily: 'monospace', fontWeight: 700 }} />
        <StatusChip status={order.status} />
      </Stack>
      <Typography variant="caption" color="text.secondary" sx={{ mb: 0 }}>
        {workCenterName} · Qtd planejada: <strong>{order.plannedQuantity.toLocaleString('pt-BR')}</strong>
      </Typography>

      {/* ── Status stepper ── */}
      <StatusStepper status={order.status} />

      <Divider sx={{ mb: 2 }} />

      {transitionError && (
        <Alert severity="error" onClose={() => setTransitionError(null)} sx={{ mb: 2 }}>
          {transitionError}
        </Alert>
      )}

      {/* ── Draft: próximo passo → Liberar ── */}
      {order.status.key === 'Draft' && (
        <Authorized permission="production.write">
          <NextStepCard
            color="primary"
            description="Verifique o produto, a BOM e o centro de trabalho. Libere a ordem para reservar os materiais e autorizar a execução."
          >
            <Button fullWidth variant="contained" color="primary"
              startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Zap size={16} />}
              disabled={transitioning}
              onClick={() => void doTransition(() => releaseProductionOrder(tenantCode, order.id), { key: 'Released', label: 'Liberada', color: 'info' })}
              sx={{ fontWeight: 800, mb: 1 }}>
              Liberar Ordem
            </Button>
            <Button fullWidth variant="text" color="error" size="small"
              disabled={transitioning}
              onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), { key: 'Cancelled', label: 'Cancelada', color: 'error' })}>
              Cancelar Ordem
            </Button>
          </NextStepCard>
        </Authorized>
      )}

      {/* ── Released: próximo passo → Iniciar Execução ── */}
      {order.status.key === 'Released' && (
        <Authorized permission="production.write">
          <NextStepCard
            color="success"
            description="Materiais reservados. Confirme que o operador está pronto e inicie a execução na linha de produção."
          >
            <Button fullWidth variant="contained" color="success"
              startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Play size={16} />}
              disabled={transitioning}
              onClick={() => void doTransition(() => startOrderExecution(tenantCode, order.id), { key: 'InExecution', label: 'Em Execução', color: 'warning' })}
              sx={{ fontWeight: 800, mb: 1 }}>
              Iniciar Execução
            </Button>
            <Button fullWidth variant="text" color="error" size="small"
              disabled={transitioning}
              onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), { key: 'Cancelled', label: 'Cancelada', color: 'error' })}>
              Cancelar Ordem
            </Button>
          </NextStepCard>
        </Authorized>
      )}

      {/* ── InExecution: registros inline + concluir ── */}
      {order.status.key === 'InExecution' && (
        <>
          {/* BOM reference */}
          {bom && bom.items.length > 0 && (
            <Section
              title={`Itens da BOM (${bom.items.length})`}
              icon={<ClipboardCheck size={13} color="#666" />}
              defaultExpanded
            >
              <Box component="table" sx={{ width: '100%', borderCollapse: 'collapse' }}>
                <Box component="thead">
                  <Box component="tr">
                    {['MATERIAL', 'QTD TOTAL', 'UM'].map(h => (
                      <Box component="th" key={h} sx={{ py: 0.5, textAlign: h === 'QTD TOTAL' ? 'right' : 'left', fontSize: '0.65rem', fontWeight: 800, color: 'text.secondary', textTransform: 'uppercase' }}>
                        {h}
                      </Box>
                    ))}
                  </Box>
                </Box>
                <Box component="tbody">
                  {bom.items.map(item => (
                    <Box component="tr" key={item.id}>
                      <Box component="td" sx={{ py: 0.5, fontFamily: 'monospace', fontSize: '0.8rem', fontWeight: 600 }}>{item.materialCode}</Box>
                      <Box component="td" sx={{ py: 0.5, textAlign: 'right', fontWeight: 700 }}>
                        {(item.quantity * order.plannedQuantity).toLocaleString('pt-BR', { maximumFractionDigits: 4 })}
                      </Box>
                      <Box component="td" sx={{ py: 0.5, pl: 1, color: 'text.secondary', fontSize: '0.75rem' }}>{item.unitOfMeasure}</Box>
                    </Box>
                  ))}
                </Box>
              </Box>
            </Section>
          )}

          {/* Consumo */}
          <Section title="Registrar Consumo" icon={<FlaskConical size={13} color="#1565c0" />} defaultExpanded>
            <MaterialExecutionForm tenantCode={tenantCode} orderId={order.id} mode="consumption" onRecorded={() => void loadHistory()} />
          </Section>

          {/* Scrap */}
          <Section title="Registrar Scrap" icon={<Trash2 size={13} color="#e65100" />}>
            <MaterialExecutionForm tenantCode={tenantCode} orderId={order.id} mode="scrap" onRecorded={() => void loadHistory()} />
          </Section>

          {/* Inspeção */}
          <Section title="Inspeção de Qualidade" icon={<CheckCircle size={13} color="#2e7d32" />}>
            <InspectionForm tenantCode={tenantCode} orderId={order.id} onRecorded={() => void loadHistory()} />
          </Section>

          {/* Concluir */}
          <Authorized permission="production.write">
            <NextStepCard
              color="success"
              description="Registre todo o consumo de materiais e realize a inspeção de qualidade antes de concluir."
            >
              <Button fullWidth variant="contained" color="success"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <CheckCircle size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => completeProductionOrder(tenantCode, order.id), { key: 'Completed', label: 'Concluída', color: 'success' })}
                sx={{ fontWeight: 800 }}>
                Concluir Ordem
              </Button>
            </NextStepCard>
          </Authorized>
        </>
      )}

      {/* ── Completed / Cancelled ── */}
      {!isActive && (
        <Alert
          severity={order.status.key === 'Completed' ? 'success' : 'error'}
          sx={{ mb: 2 }}
        >
          {order.status.key === 'Completed'
            ? 'Ordem concluída com sucesso.'
            : 'Esta ordem foi cancelada.'}
        </Alert>
      )}

      {/* ── Histórico (lazy load on expand) ── */}
      <Section title="Histórico de Execução" icon={<History size={13} color="#666" />}>
        {history === null && !historyLoading && !historyError ? (
          <Button size="small" variant="text" onClick={() => void loadHistory()}>
            Carregar histórico
          </Button>
        ) : (
          <ExecutionHistory
            history={history}
            loading={historyLoading}
            error={historyError}
            onRefresh={() => void loadHistory()}
          />
        )}
      </Section>

    </Box>
  );
}
