import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Paper,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Tooltip, IconButton, Typography,
} from '@mui/material';
import { CheckCircle, Package, Plus, Truck } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { transitionDispatch } from '../api/logistics';
import { useDispatches } from '../hooks/useDispatches';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import { useCarriers } from '../hooks/useCarriers';
import { CreateDispatchModal } from './CreateDispatchModal';
import { FiscalStatusCell } from './FiscalStatusCell';
import type { Dispatch, DispatchStatus } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { TechnicalIdFormatter, CurrencyFormatter } from '../../../shared/lib/utils/formatters';

type Props = { tenantCode: string };
type ConfirmAction = { dispatchId: string; action: string; label: string };

const STATUS_LABEL: Record<DispatchStatus, string> = {
  Pending: 'Pendente',
  InTransit: 'Em Trânsito',
  Delivered: 'Entregue',
  Returned: 'Devolvido',
};

const STATUS_COLOR: Record<DispatchStatus, 'default' | 'warning' | 'success' | 'error'> = {
  Pending: 'default',
  InTransit: 'warning',
  Delivered: 'success',
  Returned: 'error',
};

export function DispatchesPage({ tenantCode }: Props) {
  const { data: dispatchData, loading, error: fetchError } = useDispatches(tenantCode);
  const { data: orders } = useShipmentOrders(tenantCode);
  const { data: carriers } = useCarriers(tenantCode);

  const [dispatches, setDispatches] = useState<Dispatch[]>([]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (dispatchData) setDispatches(dispatchData); }, [dispatchData]);

  const orderMap = useMemo(
    () => new Map(orders?.map(o => [o.id, o]) ?? []),
    [orders]
  );
  const carrierMap = useMemo(
    () => new Map(carriers?.map(c => [c.id, c]) ?? []),
    [carriers]
  );

  const handleCreated = (dispatch: Dispatch) => {
    setDispatches(prev => [dispatch, ...prev]);
    setCreateOpen(false);
    setSuccess(`Despacho ${dispatch.trackingCode} criado.`);
  };

  const handleTransition = (dispatchId: string, action: string, label: string) => {
    setConfirm({ dispatchId, action, label });
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true); setMutationError(null); setSuccess(null);
    try {
      await transitionDispatch(tenantCode, confirm.dispatchId, confirm.action);
      const actionToStatus: Record<string, DispatchStatus> = {
        conference: 'Pending',
        ship: 'InTransit',
        deliver: 'Delivered',
      };
      const newStatus = actionToStatus[confirm.action];
      if (newStatus) {
        setDispatches(prev => prev.map(d =>
          d.id === confirm.dispatchId ? { ...d, status: newStatus } : d
        ));
      }
      setSuccess(`"${confirm.label}" realizado com sucesso.`);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao atualizar despacho.'));
    } finally {
      setConfirming(false);
      setConfirm(null);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Despachos" icon={<Truck size={20} />} />

      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
        <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
          Novo Despacho
        </Button>
      </Box>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Rastreio</TableCell>
              <TableCell>Ordem</TableCell>
              <TableCell>Transportadora</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>NF-e</TableCell>
              <TableCell align="right">Frete (R$)</TableCell>
              <TableCell>Criado em</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {dispatches.length === 0 && (
              <TableRow>
                <TableCell colSpan={8} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  Nenhum despacho cadastrado.
                </TableCell>
              </TableRow>
            )}
            {dispatches.map(d => {
              const order = orderMap.get(d.shipmentOrderId);
              const carrier = carrierMap.get(d.carrierId);
              return (
                <TableRow key={d.id} hover>
                  <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: 13 }}>
                    {d.trackingCode}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                      {order?.orderNumber ?? '—'}
                    </Typography>
                    {order?.recipientName && (
                      <Typography variant="caption" color="text.secondary">{order.recipientName}</Typography>
                    )}
                  </TableCell>
                  <TableCell>{carrier?.name ?? '—'}</TableCell>
                  <TableCell>
                    <Chip
                      label={STATUS_LABEL[d.status]}
                      color={STATUS_COLOR[d.status]}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <FiscalStatusCell status={d.fiscalStatus} accessKey={d.fiscalAccessKey} externalId={d.fiscalExternalId} errorMessage={d.fiscalErrorMessage} />
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" sx={{ fontWeight: 800 }}>
                      {CurrencyFormatter.format(d.freightValueBrl)}
                    </Typography>
                  </TableCell>
                  <TableCell>{new Date(d.createdAt).toLocaleDateString('pt-BR')}</TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    {d.status === 'Pending' && !d.conferencedAt && (
                      <Tooltip title="Conferir">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => handleTransition(d.id, 'conference', 'Conferir')}
                        >
                          <CheckCircle size={15} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {d.status === 'Pending' && d.conferencedAt && (
                      <Tooltip title="Despachar (emite NF-e automaticamente)">
                        <Button
                          size="small"
                          variant="contained"
                          color="warning"
                          startIcon={<Package size={13} />}
                          onClick={() => handleTransition(d.id, 'ship', 'Despachar')}
                          sx={{ fontSize: 12 }}
                        >
                          Despachar
                        </Button>
                      </Tooltip>
                    )}
                    {d.status === 'InTransit' && (
                      <Tooltip title="Marcar como Entregue">
                        <Button
                          size="small"
                          variant="outlined"
                          color="success"
                          startIcon={<CheckCircle size={13} />}
                          onClick={() => handleTransition(d.id, 'deliver', 'Entregar')}
                          sx={{ fontSize: 12 }}
                        >
                          Entregue
                        </Button>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <CreateDispatchModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      <ConfirmDialog
        open={!!confirm}
        title="Confirmar ação"
        message={
          confirm?.action === 'ship'
            ? 'Ao despachar, a NF-e será emitida automaticamente pelo sistema fiscal configurado.'
            : `Confirmar: "${confirm?.label}"?`
        }
        confirmLabel={confirm?.label ?? 'Confirmar'}
        severity={confirm?.action === 'ship' ? 'warning' : 'primary'}
        loading={confirming}
        onConfirm={handleConfirm}
        onCancel={() => setConfirm(null)}
      />
    </Box>
  );
}
