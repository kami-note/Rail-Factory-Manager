import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
  alpha,
  useTheme
} from '@mui/material';
import {
  ClipboardList,
  Play,
  CheckCircle,
  XCircle,
  Plus,
  Zap,
  FlaskConical,
  Trash2,
  RefreshCw,
  History,
  Search
} from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { InlineError } from '../../../shared/components/common/InlineError';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { Authorized } from '../../auth';
import {
  listProductionOrders,
  listWorkCenters,
  listBoms,
  createProductionOrder,
  releaseProductionOrder,
  startOrderExecution,
  cancelProductionOrder,
  completeProductionOrder,
  recordConsumption,
  recordScrap,
  recordInspection,
  getOrderExecutionHistory
} from '../api/production';
import type { ProductionOrder, WorkCenter, Bom, OrderExecutionHistory } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

const STATUS_OPTIONS = [
  { value: '', label: 'Todos' },
  { value: 'Draft', label: 'Rascunho' },
  { value: 'Released', label: 'Liberada' },
  { value: 'InExecution', label: 'Em Execução' },
  { value: 'Completed', label: 'Concluída' },
  { value: 'Cancelled', label: 'Cancelada' },
];

type ProductionOrdersPageProps = {
  tenantCode: string;
};

export function ProductionOrdersPage({ tenantCode }: ProductionOrdersPageProps) {
  const theme = useTheme();
  const [orders, setOrders] = useState<ProductionOrder[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState('');
  const [workCenterFilter, setWorkCenterFilter] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);

  const selectedOrder = useMemo(() =>
    orders.find(o => o.id === selectedOrderId),
    [orders, selectedOrderId]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [ordersData, wcData] = await Promise.all([
        listProductionOrders(tenantCode, statusFilter || undefined, workCenterFilter || undefined),
        listWorkCenters(tenantCode)
      ]);
      setOrders(ordersData);
      setWorkCenters(wcData);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar as ordens de produção.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [tenantCode, statusFilter, workCenterFilter]);

  const updateOrderStatus = (id: string, status: ProductionOrder['status']) => {
    setOrders(prev => prev.map(o => o.id === id ? { ...o, status } : o));
  };

  const handleTransition = async (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => {
    setError(null);
    try {
      await action();
      updateOrderStatus(id, newStatus);
    } catch (err) {
      setError(toUiErrorMessage(err, 'A operação não pôde ser concluída.'));
    }
  };

  const workCenterName = (id: string) => workCenters.find(wc => wc.id === id)?.name ?? id.slice(0, 8);

  return (
    <Box sx={{ height: 'calc(100vh - 140px)', display: 'flex', flexDirection: 'column', p: 3 }}>
      <ModuleHeader
        label="ORDENS DE PRODUÇÃO"
        icon={<ClipboardList size={20} />}
        action={
          <Authorized permission="production.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setShowCreateForm(v => !v)}>
              Nova Ordem
            </Button>
          </Authorized>
        }
      />

      {error && <InlineError message={error} marginBottom={2} />}

      {showCreateForm && (
        <CreateOrderForm
          tenantCode={tenantCode}
          workCenters={workCenters}
          onCreated={order => { setOrders(prev => [order, ...prev]); setShowCreateForm(false); setSelectedOrderId(order.id); }}
          onCancel={() => setShowCreateForm(false)}
        />
      )}

      <Stack direction="row" spacing={2} sx={{ my: 2, alignItems: 'center' }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Status</InputLabel>
          <Select label="Status" value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
            {STATUS_OPTIONS.map(s => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel>Centro de Trabalho</InputLabel>
          <Select label="Centro de Trabalho" value={workCenterFilter} onChange={e => setWorkCenterFilter(e.target.value)}>
            <MenuItem value="">Todos</MenuItem>
            {workCenters.filter(wc => wc.status === 'Active').map(wc => (
              <MenuItem key={wc.id} value={wc.id}>{wc.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <Button size="small" startIcon={<RefreshCw size={14} />} onClick={() => void load()}>
          Atualizar
        </Button>
      </Stack>

      <Box sx={{ flexGrow: 1, overflow: 'hidden', display: 'flex', gap: 2 }}>
        <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
          {loading ? (
            <Box sx={{ textAlign: 'center', pt: 6 }}><CircularProgress size={32} /></Box>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table stickyHeader size="small">
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 800 }}>Nº ORDEM</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>PRODUTO</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>CENTRO</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>QTD PLANEJADA</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {orders.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                        Nenhuma ordem encontrada.
                      </TableCell>
                    </TableRow>
                  ) : orders.map(order => (
                    <TableRow
                      key={order.id}
                      hover
                      selected={selectedOrderId === order.id}
                      onClick={() => setSelectedOrderId(order.id)}
                      sx={{ cursor: 'pointer' }}
                    >
                      <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{order.orderNumber}</TableCell>
                      <TableCell sx={{ fontWeight: 600 }}>{order.productCode}</TableCell>
                      <TableCell sx={{ color: 'text.secondary' }}>{workCenterName(order.workCenterId)}</TableCell>
                      <TableCell align="right">{order.plannedQuantity}</TableCell>
                      <TableCell><StatusChip status={order.status} /></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Box>

        {selectedOrder && (
          <Paper
            variant="outlined"
            sx={{
              width: 400,
              flexShrink: 0,
              overflowY: 'auto',
              bgcolor: 'background.paper',
              borderLeft: 2,
              borderColor: alpha(theme.palette.primary.main, 0.2)
            }}
          >
            <ExecutionPanel
              key={selectedOrder.id}
              tenantCode={tenantCode}
              order={selectedOrder}
              onTransition={handleTransition}
            />
          </Paper>
        )}
      </Box>
    </Box>
  );
}

function ExecutionPanel({ tenantCode, order, onTransition }: {
  tenantCode: string;
  order: ProductionOrder;
  onTransition: (action: () => Promise<void>, id: string, newStatus: ProductionOrder['status']) => Promise<void>;
}) {
  // 0 = Ações, 1 = Registros, 2 = Histórico
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
    ...(order.status === 'InExecution' ? [{ label: 'Registrar' }] : []),
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
            {order.status === 'Draft' && (
              <Button fullWidth variant="contained" color="primary"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Zap size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => releaseProductionOrder(tenantCode, order.id), 'Released')}
                sx={{ fontWeight: 800 }}>
                Liberar Ordem
              </Button>
            )}
            {order.status === 'Released' && (
              <Button fullWidth variant="contained" color="success"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <Play size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => startOrderExecution(tenantCode, order.id), 'InExecution')}
                sx={{ fontWeight: 800 }}>
                Iniciar Execução
              </Button>
            )}
            {order.status === 'InExecution' && (
              <Button fullWidth variant="contained" color="success"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <CheckCircle size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => completeProductionOrder(tenantCode, order.id), 'Completed')}
                sx={{ fontWeight: 800 }}>
                Concluir Ordem
              </Button>
            )}
            {(order.status === 'Draft' || order.status === 'Released') && (
              <Button fullWidth variant="outlined" color="error"
                startIcon={transitioning ? <CircularProgress size={16} color="inherit" /> : <XCircle size={16} />}
                disabled={transitioning}
                onClick={() => void doTransition(() => cancelProductionOrder(tenantCode, order.id), 'Cancelled')}>
                Cancelar Ordem
              </Button>
            )}
            {(order.status === 'Completed' || order.status === 'Cancelled') && (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                Esta ordem está encerrada.
              </Typography>
            )}
          </Stack>
        </Authorized>
      )}

      {/* Tab: Registrar (só em execução) */}
      {tab === 1 && order.status === 'InExecution' && (
        <ExecutionForms tenantCode={tenantCode} orderId={order.id} onRecorded={() => void loadHistory()} />
      )}

      {/* Tab: Histórico */}
      {tab === (order.status === 'InExecution' ? 2 : 1) && (
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

function ExecutionForms({ tenantCode, orderId, onRecorded }: {
  tenantCode: string;
  orderId: string;
  onRecorded: () => void;
}) {
  const [subTab, setSubTab] = useState(0);

  return (
    <>
      <Tabs value={subTab} onChange={(_, v) => setSubTab(v as number)} variant="fullWidth" sx={{ mb: 2 }}>
        <Tab label="Consumo" icon={<FlaskConical size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Scrap" icon={<Trash2 size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
        <Tab label="Inspeção" icon={<CheckCircle size={13} />} iconPosition="start" sx={{ minHeight: 36, fontSize: '0.7rem' }} />
      </Tabs>
      {subTab === 0 && <ConsumptionForm tenantCode={tenantCode} orderId={orderId} onRecorded={onRecorded} />}
      {subTab === 1 && <ScrapForm tenantCode={tenantCode} orderId={orderId} onRecorded={onRecorded} />}
      {subTab === 2 && <InspectionForm tenantCode={tenantCode} orderId={orderId} onRecorded={onRecorded} />}
    </>
  );
}

function ExecutionHistory({ history, loading, error, onRefresh }: {
  history: OrderExecutionHistory | null;
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}) {
  if (loading) return <Box sx={{ textAlign: 'center', py: 4 }}><CircularProgress size={24} /></Box>;
  if (error) return <Alert severity="error" action={<Button size="small" onClick={onRefresh}>Tentar novamente</Button>}>{error}</Alert>;
  if (!history) return null;

  const isEmpty = history.consumptions.length === 0 && history.scraps.length === 0 && history.inspections.length === 0;

  if (isEmpty) return (
    <Box sx={{ textAlign: 'center', py: 4 }}>
      <Typography variant="body2" color="text.secondary">Nenhum registro de execução.</Typography>
    </Box>
  );

  return (
    <Stack spacing={3}>
      {history.consumptions.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'primary.main', display: 'block', mb: 1 }}>
            CONSUMOS ({history.consumptions.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.consumptions.map((c, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{c.materialCode}</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{c.consumedQuantity} {c.unitOfMeasure}</Typography>
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {new Date(c.recordedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      {history.scraps.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'warning.main', display: 'block', mb: 1 }}>
            SCRAP ({history.scraps.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.scraps.map((s, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{s.materialCode}</Typography>
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>{s.scrapQuantity} {s.unitOfMeasure}</Typography>
                </Stack>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>{s.reason}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {new Date(s.recordedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      {history.inspections.length > 0 && (
        <Box>
          <Typography variant="caption" sx={{ fontWeight: 800, color: 'text.secondary', display: 'block', mb: 1 }}>
            INSPEÇÕES ({history.inspections.length})
          </Typography>
          <Stack spacing={0.5}>
            {history.inspections.map((ins, i) => (
              <Paper key={i} variant="outlined" sx={{ p: 1.5, borderColor: ins.result === 'Passed' ? 'success.light' : 'error.light' }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <Chip
                    size="small"
                    label={ins.result === 'Passed' ? 'Aprovado' : 'Reprovado'}
                    color={ins.result === 'Passed' ? 'success' : 'error'}
                    variant="filled"
                    sx={{ height: 20, fontSize: '0.65rem' }}
                  />
                  <Typography variant="caption" color="text.secondary">{ins.inspectedBy}</Typography>
                </Stack>
                {ins.notes && <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>{ins.notes}</Typography>}
                <Typography variant="caption" color="text.disabled">
                  {new Date(ins.inspectedAt).toLocaleString('pt-BR')}
                </Typography>
              </Paper>
            ))}
          </Stack>
        </Box>
      )}

      <Button size="small" startIcon={<RefreshCw size={13} />} onClick={onRefresh} sx={{ alignSelf: 'flex-end' }}>
        Atualizar
      </Button>
    </Stack>
  );
}

function ConsumptionForm({ tenantCode, orderId, onRecorded }: { tenantCode: string; orderId: string; onRecorded: () => void }) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('');
  const [unit, setUnit] = useState('UN');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async () => {
    setSaving(true); setError(null); setSuccess(false);
    try {
      await recordConsumption(tenantCode, orderId, { materialCode: materialCode.trim().toUpperCase(), consumedQuantity: Number(quantity), unitOfMeasure: unit.trim().toUpperCase() });
      setSuccess(true); setMaterialCode(''); setQuantity('');
      onRecorded();
    } catch (err) { setError(toUiErrorMessage(err, 'Não foi possível registrar o consumo.')); }
    finally { setSaving(false); }
  };

  return (
    <Stack spacing={2}>
      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)}>Consumo registrado.</Alert>}
      <TextField label="Código do material" size="small" fullWidth value={materialCode} onChange={e => setMaterialCode(e.target.value.toUpperCase())} slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }} />
      <Stack direction="row" spacing={1}>
        <TextField label="Quantidade" type="number" size="small" sx={{ flexGrow: 1 }} value={quantity} onChange={e => setQuantity(e.target.value)} />
        <TextField label="Unidade" size="small" sx={{ width: 80 }} value={unit} onChange={e => setUnit(e.target.value.toUpperCase())} />
      </Stack>
      <Authorized permission="production.write">
        <Button variant="contained" fullWidth onClick={() => void handleSubmit()} disabled={saving || !materialCode.trim() || !quantity} sx={{ fontWeight: 800 }}>
          {saving ? <CircularProgress size={18} color="inherit" /> : 'Registrar Consumo'}
        </Button>
      </Authorized>
    </Stack>
  );
}

function ScrapForm({ tenantCode, orderId, onRecorded }: { tenantCode: string; orderId: string; onRecorded: () => void }) {
  const [materialCode, setMaterialCode] = useState('');
  const [quantity, setQuantity] = useState('');
  const [unit, setUnit] = useState('UN');
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async () => {
    setSaving(true); setError(null); setSuccess(false);
    try {
      await recordScrap(tenantCode, orderId, { materialCode: materialCode.trim().toUpperCase(), scrapQuantity: Number(quantity), unitOfMeasure: unit.trim().toUpperCase(), reason: reason.trim() });
      setSuccess(true); setMaterialCode(''); setQuantity(''); setReason('');
      onRecorded();
    } catch (err) { setError(toUiErrorMessage(err, 'Não foi possível registrar o scrap.')); }
    finally { setSaving(false); }
  };

  return (
    <Stack spacing={2}>
      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)}>Scrap registrado.</Alert>}
      <TextField label="Código do material" size="small" fullWidth value={materialCode} onChange={e => setMaterialCode(e.target.value.toUpperCase())} slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }} />
      <Stack direction="row" spacing={1}>
        <TextField label="Quantidade" type="number" size="small" sx={{ flexGrow: 1 }} value={quantity} onChange={e => setQuantity(e.target.value)} />
        <TextField label="Unidade" size="small" sx={{ width: 80 }} value={unit} onChange={e => setUnit(e.target.value.toUpperCase())} />
      </Stack>
      <TextField label="Motivo" size="small" fullWidth multiline rows={2} value={reason} onChange={e => setReason(e.target.value)} />
      <Authorized permission="production.write">
        <Button variant="contained" color="warning" fullWidth onClick={() => void handleSubmit()} disabled={saving || !materialCode.trim() || !quantity || !reason.trim()} sx={{ fontWeight: 800 }}>
          {saving ? <CircularProgress size={18} color="inherit" /> : 'Registrar Scrap'}
        </Button>
      </Authorized>
    </Stack>
  );
}

function InspectionForm({ tenantCode, orderId, onRecorded }: { tenantCode: string; orderId: string; onRecorded: () => void }) {
  const [result, setResult] = useState<'Passed' | 'Failed'>('Passed');
  const [inspectedBy, setInspectedBy] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async () => {
    setSaving(true); setError(null); setSuccess(false);
    try {
      await recordInspection(tenantCode, orderId, { result, inspectedBy: inspectedBy.trim(), notes: notes.trim() || undefined });
      setSuccess(true); setInspectedBy(''); setNotes('');
      onRecorded();
    } catch (err) { setError(toUiErrorMessage(err, 'Não foi possível registrar a inspeção.')); }
    finally { setSaving(false); }
  };

  return (
    <Stack spacing={2}>
      {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
      {success && <Alert severity="success" onClose={() => setSuccess(false)}>Inspeção registrada.</Alert>}
      <Stack direction="row" spacing={1}>
        <Button fullWidth variant={result === 'Passed' ? 'contained' : 'outlined'} color="success" onClick={() => setResult('Passed')} startIcon={<CheckCircle size={15} />} sx={{ fontWeight: 800 }}>Aprovado</Button>
        <Button fullWidth variant={result === 'Failed' ? 'contained' : 'outlined'} color="error" onClick={() => setResult('Failed')} startIcon={<XCircle size={15} />} sx={{ fontWeight: 800 }}>Reprovado</Button>
      </Stack>
      <TextField label="Inspecionado por" size="small" fullWidth value={inspectedBy} onChange={e => setInspectedBy(e.target.value)} />
      <TextField label="Observações (opcional)" size="small" fullWidth multiline rows={2} value={notes} onChange={e => setNotes(e.target.value)} />
      <Authorized permission="production.write">
        <Button variant="contained" color={result === 'Passed' ? 'success' : 'error'} fullWidth onClick={() => void handleSubmit()} disabled={saving || !inspectedBy.trim()} sx={{ fontWeight: 800 }}>
          {saving ? <CircularProgress size={18} color="inherit" /> : 'Registrar Inspeção'}
        </Button>
      </Authorized>
    </Stack>
  );
}

function CreateOrderForm({ tenantCode, workCenters, onCreated, onCancel }: {
  tenantCode: string;
  workCenters: WorkCenter[];
  onCreated: (order: ProductionOrder) => void;
  onCancel: () => void;
}) {
  const [productCode, setProductCode] = useState('');
  const [boms, setBoms] = useState<Bom[]>([]);
  const [bomsLoading, setBomsLoading] = useState(false);
  const [selectedBomId, setSelectedBomId] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [plannedQuantity, setPlannedQuantity] = useState('1');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activeWorkCenters = workCenters.filter(wc => wc.status === 'Active');

  const handleSearchBoms = async () => {
    if (!productCode.trim()) return;
    setBomsLoading(true);
    setSelectedBomId('');
    setBoms([]);
    try {
      const found = await listBoms(tenantCode, productCode.trim().toUpperCase());
      setBoms(found);
      const active = found.find(b => b.status === 'Active');
      if (active) setSelectedBomId(active.id);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível buscar as BOMs.'));
    } finally {
      setBomsLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!selectedBomId || !workCenterId || !plannedQuantity) return;
    setSaving(true);
    setError(null);
    try {
      const order = await createProductionOrder(tenantCode, { bomId: selectedBomId, workCenterId, plannedQuantity: Number(plannedQuantity) });
      onCreated(order);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar a ordem de produção.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 3, mt: 2 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>NOVA ORDEM DE PRODUÇÃO</Typography>
      {error && <InlineError message={error} marginBottom={2} />}

      <Stack spacing={2}>
        {/* Busca de BOM por produto */}
        <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
          <TextField
            label="Código do produto"
            size="small"
            sx={{ flexGrow: 1 }}
            value={productCode}
            onChange={e => setProductCode(e.target.value.toUpperCase())}
            onKeyDown={e => e.key === 'Enter' && void handleSearchBoms()}
            placeholder="Ex: TRILHO-60"
            slotProps={{ htmlInput: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
          />
          <Tooltip title="Buscar BOMs para este produto">
            <Button
              variant="outlined"
              onClick={() => void handleSearchBoms()}
              disabled={bomsLoading || !productCode.trim()}
              sx={{ alignSelf: 'center' }}
            >
              {bomsLoading ? <CircularProgress size={18} /> : <Search size={18} />}
            </Button>
          </Tooltip>
        </Stack>

        {boms.length > 0 && (
          <FormControl size="small" fullWidth>
            <InputLabel>Estrutura (BOM)</InputLabel>
            <Select label="Estrutura (BOM)" value={selectedBomId} onChange={e => setSelectedBomId(e.target.value)}>
              {boms.map(b => (
                <MenuItem key={b.id} value={b.id}>
                  v{b.version} — {b.status === 'Active' ? 'Ativa' : 'Rascunho'} — {b.items.length} componente(s)
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        )}

        {boms.length === 0 && productCode && !bomsLoading && (
          <Typography variant="caption" color="text.secondary">
            Busque pelo código do produto para selecionar uma BOM ativa.
          </Typography>
        )}

        <Stack direction="row" spacing={2}>
          <FormControl size="small" sx={{ flexGrow: 1 }}>
            <InputLabel>Centro de Trabalho</InputLabel>
            <Select label="Centro de Trabalho" value={workCenterId} onChange={e => setWorkCenterId(e.target.value)}>
              {activeWorkCenters.map(wc => <MenuItem key={wc.id} value={wc.id}>{wc.name}</MenuItem>)}
            </Select>
          </FormControl>
          <TextField
            label="Qtd Planejada"
            type="number"
            size="small"
            sx={{ width: 140 }}
            value={plannedQuantity}
            onChange={e => setPlannedQuantity(e.target.value)}
          />
        </Stack>

        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button variant="outlined" onClick={onCancel} disabled={saving}>Cancelar</Button>
          <Button
            variant="contained"
            onClick={() => void handleSubmit()}
            disabled={saving || !selectedBomId || !workCenterId || !plannedQuantity}
            startIcon={saving ? <CircularProgress size={16} color="inherit" /> : undefined}
            sx={{ fontWeight: 800 }}
          >
            Criar Ordem
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}
