import React, { useMemo, useState } from 'react';
import {
  Box, Button, Chip, CircularProgress, InputAdornment,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, IconButton, Typography,
} from '@mui/material';

import { PackageCheck, Plus, Search, XCircle } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { transitionShipmentOrder } from '../api/logistics';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import { useDispatches } from '../hooks/useDispatches';
import { useCarriers } from '../hooks/useCarriers';
import { CreateShipmentOrderModal } from './CreateShipmentOrderModal';
import { AddShipmentItemModal } from './AddShipmentItemModal';
import { FiscalStatusCell } from './FiscalStatusCell';
import { ShipmentOrderDetailPanel } from './ShipmentOrderDetailPanel';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import type { Dispatch, ShipmentItem, ShipmentOrder, ShipmentOrderStatus } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { CurrencyFormatter, RelativeDateFormatter } from '../../../shared/lib/utils/formatters';

type Props = { tenantCode: string };
type ConfirmAction = { type: string; id: string; label: string };

const STATUS_LABELS: Record<ShipmentOrderStatus, string> = {
  Draft: 'Rascunho', Picking: 'Separação', Packing: 'Embalagem',
  ReadyToShip: 'Pronto p/ Despacho', Shipped: 'Despachado', Cancelled: 'Cancelado',
};

const STATUS_COLOR: Record<ShipmentOrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Draft: 'default', Picking: 'info', Packing: 'warning',
  ReadyToShip: 'success', Shipped: 'success', Cancelled: 'error',
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'agora';
  if (mins < 60) return `${mins}min`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h`;
  return `${Math.floor(hours / 24)}d`;
}

function orderTotalValue(items: ShipmentItem[]): number | null {
  if (items.length === 0 || items.every(i => !i.unitValue)) return null;
  return items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);
}

function orderTotalWeight(items: ShipmentItem[]): number {
  return items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
}

const KPI_CONFIG = [
  { label: 'Rascunho', statuses: ['Draft'] as ShipmentOrderStatus[], color: '#757575' },
  { label: 'Em andamento', statuses: ['Picking', 'Packing'] as ShipmentOrderStatus[], color: '#0288d1' },
  { label: 'Pronto p/ Despacho', statuses: ['ReadyToShip'] as ShipmentOrderStatus[], color: '#388e3c' },
  { label: 'Despachado', statuses: ['Shipped'] as ShipmentOrderStatus[], color: '#1b5e20' },
];

export function ShipmentOrdersPage({ tenantCode }: Props) {
  const { data, loading, error: fetchError } = useShipmentOrders(tenantCode);
  const { data: dispatchData } = useDispatches(tenantCode);
  const { data: carriersData } = useCarriers(tenantCode);
  const [localOrders, setLocalOrders] = useState<ShipmentOrder[] | null>(null);
  const orders = localOrders ?? data ?? [];
  const [filterStatus, setFilterStatus] = useState<ShipmentOrderStatus | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const statusCounts = useMemo(() => {
    const counts: Partial<Record<ShipmentOrderStatus, number>> = {};
    orders.forEach(o => { counts[o.status] = (counts[o.status] ?? 0) + 1; });
    return counts;
  }, [orders]);

  const filteredOrders = useMemo(() => {
    let result = filterStatus ? orders.filter(o => o.status === filterStatus) : orders;
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      result = result.filter(o =>
        o.orderNumber.toLowerCase().includes(q) ||
        o.recipientName?.toLowerCase().includes(q) ||
        o.recipientCnpj?.includes(q)
      );
    }
    return result;
  }, [orders, filterStatus, searchQuery]);

  const dispatchByOrder = useMemo(() => {
    const map = new Map<string, Dispatch>();
    dispatchData?.forEach(d => map.set(d.shipmentOrderId, d));
    return map;
  }, [dispatchData]);

  const carrierById = useMemo(() => {
    const map = new Map<string, string>();
    carriersData?.forEach(c => map.set(c.id, c.name));
    return map;
  }, [carriersData]);

  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [detailOrder, setDetailOrder] = useState<ShipmentOrder | null>(null);
  const [addItemOrder, setAddItemOrder] = useState<ShipmentOrder | null>(null);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [processingId, setProcessingId] = useState<string | null>(null);

  const handleCreated = (o: ShipmentOrder) => {
    setLocalOrders(prev => [o, ...(prev ?? data ?? [])]);
    setCreateOpen(false);
    setSuccess(`Ordem "${o.orderNumber}" criada com sucesso.`);
  };

  const handleItemAdded = (orderId: string, item: ShipmentItem) => {
    setLocalOrders(prev => (prev ?? data ?? []).map(o =>
      o.id === orderId ? { ...o, items: [...o.items, item] } : o
    ));
    setAddItemOrder(null);
    setSuccess('Item adicionado com sucesso.');
  };

  const handleTransition = (id: string, action: string, label: string) => {
    setConfirm({ type: action, id, label });
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    const { id, type, label } = confirm;
    setMutationError(null); setSuccess(null);
    setConfirm(null);
    setProcessingId(id);
    try {
      await transitionShipmentOrder(tenantCode, id, type);
      const actionToStatus: Record<string, ShipmentOrderStatus> = {
        'start-picking': 'Picking', 'start-packing': 'Packing',
        'ready-to-ship': 'ReadyToShip', 'cancel': 'Cancelled',
      };
      const newStatus = actionToStatus[type];
      if (newStatus) setLocalOrders(prev => (prev ?? data ?? []).map(o => o.id === id ? { ...o, status: newStatus } : o));
      setSuccess(`Operação "${label}" realizada.`);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao alterar status.'));
    } finally {
      setProcessingId(null);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="Ordens de Expedição"
        icon={<PackageCheck size={20} />}
        action={
          <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
            Nova Ordem
          </Button>
        }
      />

      {/* KPI Cards */}
      <Stack direction="row" spacing={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
        {KPI_CONFIG.map(kpi => {
          const value = kpi.statuses.reduce((s, st) => s + (statusCounts[st] ?? 0), 0);
          return (
            <Paper
              key={kpi.label}
              elevation={0}
              sx={{
                px: 2.5, py: 1.5, border: 1, borderColor: 'divider',
                borderRadius: 2, minWidth: 130, flex: '1 1 130px',
              }}
            >
              <Typography variant="h4" sx={{ fontWeight: 800, color: kpi.color, lineHeight: 1 }}>
                {value}
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 500 }}>
                {kpi.label}
              </Typography>
            </Paper>
          );
        })}
      </Stack>

      {/* Filtros por status + Busca */}
      <Stack direction="row" sx={{ mb: 2, alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
        <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 1, flex: 1 }}>
          <Chip
            label={`Todos (${orders.length})`}
            size="small"
            onClick={() => setFilterStatus(null)}
            color={filterStatus === null ? 'primary' : 'default'}
            variant={filterStatus === null ? 'filled' : 'outlined'}
          />
          {(Object.keys(STATUS_LABELS) as ShipmentOrderStatus[]).map(s => {
            const count = statusCounts[s] ?? 0;
            if (count === 0) return null;
            return (
              <Chip
                key={s}
                label={`${STATUS_LABELS[s]} (${count})`}
                size="small"
                onClick={() => setFilterStatus(filterStatus === s ? null : s)}
                color={filterStatus === s ? STATUS_COLOR[s] : 'default'}
                variant={filterStatus === s ? 'filled' : 'outlined'}
              />
            );
          })}
        </Stack>
        <TextField
          size="small"
          placeholder="Buscar por número, destinatário..."
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
          sx={{ minWidth: 260 }}
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><Search size={15} /></InputAdornment> } }}
        />
      </Stack>

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Número</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>NF-e</TableCell>
              <TableCell align="right">Itens</TableCell>
              <TableCell align="right">Peso (kg)</TableCell>
              <TableCell align="right">Valor Total</TableCell>
              <TableCell>Destinatário</TableCell>
              <TableCell>Criado em</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {filteredOrders.length === 0 && (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  {searchQuery
                    ? 'Nenhuma ordem encontrada para esta busca.'
                    : filterStatus
                    ? `Nenhuma ordem com status "${STATUS_LABELS[filterStatus]}".`
                    : 'Nenhuma ordem cadastrada.'}
                </TableCell>
              </TableRow>
            )}
            {filteredOrders.map(o => {
              const dispatch = dispatchByOrder.get(o.id);
              const isProcessing = processingId === o.id;
              const totalWeight = orderTotalWeight(o.items);
              const totalValue = orderTotalValue(o.items);
              return (
                <TableRow
                  key={o.id}
                  hover
                  onClick={() => setDetailOrder(o)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{o.orderNumber}</TableCell>
                  <TableCell>
                    <Stack spacing={0.3}>
                      <Chip label={STATUS_LABELS[o.status]} color={STATUS_COLOR[o.status]} size="small" />
                      <Typography variant="caption" color="text.disabled" sx={{ fontSize: 10 }}>
                        {timeAgo(o.updatedAt)}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <FiscalStatusCell
                      status={dispatch?.fiscalStatus}
                      accessKey={dispatch?.fiscalAccessKey}
                      externalId={dispatch?.fiscalExternalId}
                      errorMessage={dispatch?.fiscalErrorMessage}
                    />
                  </TableCell>
                  <TableCell align="right">{o.items.length}</TableCell>
                  <TableCell align="right">
                    <Typography variant="caption">
                      {totalWeight > 0 ? totalWeight.toFixed(1) : '—'}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="caption">
                      {totalValue !== null ? CurrencyFormatter.format(totalValue) : '—'}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ fontSize: 12 }}>{o.recipientName ?? o.recipientCnpj ?? '-'}</TableCell>
                  <TableCell>{RelativeDateFormatter.format(o.createdAt, false)}</TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }} onClick={e => e.stopPropagation()}>
                    {o.status === 'Draft' && (
                      <Tooltip title="Adicionar Item">
                        <IconButton size="small" onClick={() => setAddItemOrder(o)}>
                          <Plus size={15} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {o.status === 'Draft' && (
                      <Tooltip title="Iniciar Separação">
                        <Button size="small" disabled={isProcessing}
                          startIcon={isProcessing ? <CircularProgress size={12} color="inherit" /> : undefined}
                          onClick={() => handleTransition(o.id, 'start-picking', 'Iniciar Separação')}>
                          {isProcessing ? '...' : 'Separar'}
                        </Button>
                      </Tooltip>
                    )}
                    {o.status === 'Picking' && (
                      <Tooltip title="Iniciar Embalagem">
                        <Button size="small" disabled={isProcessing}
                          startIcon={isProcessing ? <CircularProgress size={12} color="inherit" /> : undefined}
                          onClick={() => handleTransition(o.id, 'start-packing', 'Iniciar Embalagem')}>
                          {isProcessing ? '...' : 'Embalar'}
                        </Button>
                      </Tooltip>
                    )}
                    {o.status === 'Packing' && (
                      <Tooltip title="Marcar Pronto p/ Despacho">
                        <Button size="small" color="success" disabled={isProcessing}
                          startIcon={isProcessing ? <CircularProgress size={12} color="inherit" /> : undefined}
                          onClick={() => handleTransition(o.id, 'ready-to-ship', 'Pronto p/ Despacho')}>
                          {isProcessing ? '...' : 'Pronto'}
                        </Button>
                      </Tooltip>
                    )}
                    {['Draft', 'Picking', 'Packing'].includes(o.status) && (
                      <Tooltip title="Cancelar">
                        <IconButton size="small" color="error" disabled={isProcessing}
                          onClick={() => handleTransition(o.id, 'cancel', 'Cancelar')}>
                          <XCircle size={15} />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <CreateShipmentOrderModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      {detailOrder && (
        <ShipmentOrderDetailPanel
          order={orders.find(o => o.id === detailOrder.id) ?? detailOrder}
          dispatch={dispatchByOrder.get(detailOrder.id)}
          carrierName={(() => {
            const d = dispatchByOrder.get(detailOrder.id);
            return d ? carrierById.get(d.carrierId) : undefined;
          })()}
          onClose={() => setDetailOrder(null)}
          onTransition={handleTransition}
        />
      )}

      {addItemOrder && (
        <AddShipmentItemModal
          open={!!addItemOrder}
          tenantCode={tenantCode}
          orderId={addItemOrder.id}
          orderNumber={addItemOrder.orderNumber}
          onAdded={(item) => handleItemAdded(addItemOrder.id, item)}
          onClose={() => setAddItemOrder(null)}
        />
      )}

      <ConfirmDialog
        open={!!confirm}
        title="Confirmar ação"
        message={`Confirmar: "${confirm?.label}"?`}
        confirmLabel="Confirmar"
        severity="primary"
        onConfirm={handleConfirm}
        onCancel={() => setConfirm(null)}
      />

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
