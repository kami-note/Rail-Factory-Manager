import React, { useMemo, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress,
  Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Tooltip, IconButton,
} from '@mui/material';
import { PackageCheck, Plus, XCircle } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { transitionShipmentOrder } from '../api/logistics';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import { useDispatches } from '../hooks/useDispatches';
import { CreateShipmentOrderModal } from './CreateShipmentOrderModal';
import { AddShipmentItemModal } from './AddShipmentItemModal';
import { FiscalStatusCell } from './FiscalStatusCell';
import type { Dispatch, ShipmentItem, ShipmentOrder, ShipmentOrderStatus } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { RelativeDateFormatter } from '../../../shared/lib/utils/formatters';

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

export function ShipmentOrdersPage({ tenantCode }: Props) {
  const { data, loading, error: fetchError } = useShipmentOrders(tenantCode);
  const { data: dispatchData } = useDispatches(tenantCode);
  const [localOrders, setLocalOrders] = useState<ShipmentOrder[] | null>(null);
  const orders = localOrders ?? data ?? [];

  // Map shipmentOrderId → dispatch for fiscal status lookup
  const dispatchByOrder = useMemo(() => {
    const map = new Map<string, Dispatch>();
    dispatchData?.forEach(d => map.set(d.shipmentOrderId, d));
    return map;
  }, [dispatchData]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [addItemOrder, setAddItemOrder] = useState<ShipmentOrder | null>(null);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

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
    setConfirming(true); setMutationError(null); setSuccess(null);
    try {
      await transitionShipmentOrder(tenantCode, confirm.id, confirm.type);
      const actionToStatus: Record<string, ShipmentOrderStatus> = {
        'start-picking': 'Picking', 'start-packing': 'Packing',
        'ready-to-ship': 'ReadyToShip', 'cancel': 'Cancelled',
      };
      const newStatus = actionToStatus[confirm.type];
      if (newStatus) setLocalOrders(prev => (prev ?? data ?? []).map(o => o.id === confirm.id ? { ...o, status: newStatus } : o));
      setSuccess(`Operação "${confirm.label}" realizada.`);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao alterar status.'));
    } finally {
      setConfirming(false); setConfirm(null);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Ordens de Expedição" icon={<PackageCheck size={20} />} />

      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
        <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
          Nova Ordem
        </Button>
      </Box>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Número</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>NF-e</TableCell>
              <TableCell>Itens</TableCell>
              <TableCell>Destinatário</TableCell>
              <TableCell>Criado em</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {orders.length === 0 && (
              <TableRow><TableCell colSpan={7} align="center">Nenhuma ordem cadastrada.</TableCell></TableRow>
            )}
            {orders.map(o => {
              const dispatch = dispatchByOrder.get(o.id);
              return (
              <TableRow key={o.id}>
                <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{o.orderNumber}</TableCell>
                <TableCell>
                  <Chip label={STATUS_LABELS[o.status]} color={STATUS_COLOR[o.status]} size="small" />
                </TableCell>
                <TableCell>
                  <FiscalStatusCell status={dispatch?.fiscalStatus} accessKey={dispatch?.fiscalAccessKey} externalId={dispatch?.fiscalExternalId} errorMessage={dispatch?.fiscalErrorMessage} />
                </TableCell>
                <TableCell>{o.items.length}</TableCell>
                <TableCell sx={{ fontSize: 12 }}>{o.recipientName ?? o.recipientCnpj ?? '-'}</TableCell>
                <TableCell>{RelativeDateFormatter.format(o.createdAt, false)}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {o.status === 'Draft' && (
                    <Tooltip title="Adicionar Item">
                      <IconButton size="small" onClick={() => setAddItemOrder(o)}>
                        <Plus size={15} />
                      </IconButton>
                    </Tooltip>
                  )}
                  {o.status === 'Draft' && (
                    <Tooltip title="Iniciar Separação">
                      <Button size="small" onClick={() => handleTransition(o.id, 'start-picking', 'Iniciar Separação')}>Separar</Button>
                    </Tooltip>
                  )}
                  {o.status === 'Picking' && (
                    <Tooltip title="Iniciar Embalagem">
                      <Button size="small" onClick={() => handleTransition(o.id, 'start-packing', 'Iniciar Embalagem')}>Embalar</Button>
                    </Tooltip>
                  )}
                  {o.status === 'Packing' && (
                    <Tooltip title="Marcar Pronto p/ Despacho">
                      <Button size="small" color="success" onClick={() => handleTransition(o.id, 'ready-to-ship', 'Pronto p/ Despacho')}>Pronto</Button>
                    </Tooltip>
                  )}
                  {['Draft', 'Picking', 'Packing'].includes(o.status) && (
                    <Tooltip title="Cancelar">
                      <IconButton size="small" color="error" onClick={() => handleTransition(o.id, 'cancel', 'Cancelar')}>
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

      <CreateShipmentOrderModal open={createOpen} tenantCode={tenantCode} onCreated={handleCreated} onClose={() => setCreateOpen(false)} />

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
        loading={confirming}
        onConfirm={handleConfirm}
        onCancel={() => setConfirm(null)}
      />
    </Box>
  );
}
