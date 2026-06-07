import React, { useMemo, useState } from 'react';
import {
  Box, Button, Chip, CircularProgress, Paper,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TablePagination,
  Tooltip, IconButton, Typography,
} from '@mui/material';
import { AlertTriangle, CheckCircle, Package, Plus, Printer, RefreshCw, Truck } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { transitionDispatch, retryFiscalEmission } from '../api/logistics';
import { RETRYABLE_FISCAL_STATUSES } from '../types';
import { useDispatches } from '../hooks/useDispatches';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import { useCarriers } from '../hooks/useCarriers';
import { useVehicles } from '../../fleet/hooks/useVehicles';
import { usePeople } from '../../hr/hooks/usePeople';
import { CreateDispatchModal } from './CreateDispatchModal';
import { ConferenceModal } from './ConferenceModal';
import { DispatchPrintView } from './DispatchPrintView';
import { DamdfePrintView } from './DamdfePrintView';
import { FiscalStatusCell } from './FiscalStatusCell';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import type { Dispatch, DispatchStatus } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { RelativeDateFormatter, CurrencyFormatter } from '../../../shared/lib/utils/formatters';

function toIdMap<T extends { id: string }>(items: T[] | null | undefined) {
  return new Map(items?.map(x => [x.id, x]) ?? []);
}

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
  const { data: vehicles } = useVehicles(tenantCode);
  const { data: people } = usePeople(tenantCode);

  const [localDispatches, setLocalDispatches] = useState<Dispatch[] | null>(null);
  const dispatches = localDispatches ?? dispatchData ?? [];
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [conferenceDispatch, setConferenceDispatch] = useState<Dispatch | null>(null);
  const [printDispatch, setPrintDispatch] = useState<Dispatch | null>(null);
  const [damdfeDispatch, setDamdfeDispatch] = useState<Dispatch | null>(null);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);

  const orderMap   = useMemo(() => toIdMap(orders),   [orders]);
  const carrierMap = useMemo(() => toIdMap(carriers),  [carriers]);
  const vehicleMap = useMemo(() => toIdMap(vehicles),  [vehicles]);
  const personMap  = useMemo(() => toIdMap(people),    [people]);

  const handleConferenced = () => {
    if (!conferenceDispatch) return;
    setLocalDispatches(prev => (prev ?? dispatchData ?? []).map(d =>
      d.id === conferenceDispatch.id ? { ...d, conferencedAt: new Date().toISOString() } : d
    ));
    setSuccess(`Despacho ${conferenceDispatch.trackingCode} conferido.`);
    setConferenceDispatch(null);
  };

  const handleCreated = (dispatch: Dispatch) => {
    setLocalDispatches(prev => [dispatch, ...(prev ?? dispatchData ?? [])]);
    setCreateOpen(false);
    setSuccess(`Despacho ${dispatch.trackingCode} criado.`);
  };

  const handleRetryFiscal = async (dispatchId: string) => {
    setRetryingId(dispatchId);
    setMutationError(null);
    try {
      await retryFiscalEmission(tenantCode, dispatchId);
      setSuccess('Reemissão de NF-e solicitada.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao reemitir NF-e.'));
    } finally {
      setRetryingId(null);
    }
  };

  const handleTransition = (dispatchId: string, action: string, label: string) => {
    setConfirm({ dispatchId, action, label });
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    const { dispatchId, action, label } = confirm;
    setMutationError(null); setSuccess(null);
    setConfirm(null);
    setProcessingId(dispatchId);
    try {
      await transitionDispatch(tenantCode, dispatchId, action);
      const actionToStatus: Record<string, DispatchStatus> = {
        conference: 'Pending', ship: 'InTransit', deliver: 'Delivered',
      };
      const newStatus = actionToStatus[action];
      if (newStatus) {
        setLocalDispatches(prev => (prev ?? dispatchData ?? []).map(d =>
          d.id === dispatchId ? { ...d, status: newStatus } : d
        ));
      }
      setSuccess(`"${label}" realizado com sucesso.`);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao atualizar despacho.'));
    } finally {
      setProcessingId(null);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="Despachos"
        icon={<Truck size={20} />}
        action={
          <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
            Novo Despacho
          </Button>
        }
      />

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Rastreio</TableCell>
              <TableCell>Ordem</TableCell>
              <TableCell>Veículo</TableCell>
              <TableCell>Motorista</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>NF-e</TableCell>
              <TableCell>MDF-e</TableCell>
              <TableCell align="right">Frete (R$)</TableCell>
              <TableCell>Criado em</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {dispatches.length === 0 && (
              <TableRow>
                <TableCell colSpan={10} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  Nenhum despacho cadastrado.
                </TableCell>
              </TableRow>
            )}
            {dispatches.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map(d => {
              const order = orderMap.get(d.shipmentOrderId);
              const vehicle = d.vehicleId ? vehicleMap.get(d.vehicleId) : undefined;
              const driver = d.driverPersonId ? personMap.get(d.driverPersonId) : undefined;
              const isProcessing = processingId === d.id;
              const isRetrying = retryingId === d.id;
              const hasFiscalError = !!d.fiscalStatus && RETRYABLE_FISCAL_STATUSES.has(d.fiscalStatus);
              return (
                <TableRow key={d.id} hover sx={hasFiscalError ? { bgcolor: 'error.50' } : undefined}>
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
                  <TableCell>
                    {vehicle
                      ? <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>{vehicle.plate}</Typography>
                      : <Typography variant="body2" color="text.secondary">—</Typography>}
                  </TableCell>
                  <TableCell>{driver?.name ?? '—'}</TableCell>
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
                  <TableCell>
                    <FiscalStatusCell status={d.mdfeStatus ?? null} accessKey={d.mdfeAccessKey} externalId={d.mdfeExternalId} errorMessage={d.mdfeErrorMessage} label="MDF-e" />
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" sx={{ fontWeight: 800 }}>
                      {CurrencyFormatter.format(d.freightValueBrl)}
                    </Typography>
                  </TableCell>
                  <TableCell>{RelativeDateFormatter.format(d.createdAt, false)}</TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    {hasFiscalError && (
                      <Tooltip title={d.fiscalErrorMessage ? `Erro NF-e: ${d.fiscalErrorMessage}` : 'Erro na emissão da NF-e — clique para retentar'}>
                        <IconButton
                          size="small"
                          color="error"
                          disabled={isRetrying}
                          onClick={() => handleRetryFiscal(d.id)}
                        >
                          {isRetrying
                            ? <CircularProgress size={15} color="inherit" />
                            : <RefreshCw size={15} />}
                        </IconButton>
                      </Tooltip>
                    )}
                    {hasFiscalError && (
                      <Tooltip title="Erro na NF-e">
                        <AlertTriangle size={14} color="var(--mui-palette-error-main, #d32f2f)" style={{ verticalAlign: 'middle', marginRight: 4 }} />
                      </Tooltip>
                    )}
                    <Tooltip title="Imprimir Romaneio">
                      <IconButton
                        size="small"
                        onClick={() => setPrintDispatch(d)}
                      >
                        <Printer size={15} />
                      </IconButton>
                    </Tooltip>
                    {d.mdfeExternalId && (
                      <Tooltip title="Imprimir DA-MDF-e">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => setDamdfeDispatch(d)}
                        >
                          <Printer size={15} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {d.status === 'Pending' && !d.conferencedAt && (
                      <Tooltip title="Conferir itens">
                        <IconButton
                          size="small"
                          color="primary"
                          onClick={() => setConferenceDispatch(d)}
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
                          disabled={isProcessing}
                          startIcon={isProcessing ? <CircularProgress size={13} color="inherit" /> : <Package size={13} />}
                          onClick={() => handleTransition(d.id, 'ship', 'Despachar')}
                          sx={{ fontSize: 12 }}
                        >
                          {isProcessing ? 'Despachando...' : 'Despachar'}
                        </Button>
                      </Tooltip>
                    )}
                    {d.status === 'InTransit' && (
                      <Tooltip title="Marcar como Entregue">
                        <Button
                          size="small"
                          variant="outlined"
                          color="success"
                          disabled={isProcessing}
                          startIcon={isProcessing ? <CircularProgress size={13} color="inherit" /> : <CheckCircle size={13} />}
                          onClick={() => handleTransition(d.id, 'deliver', 'Entregar')}
                          sx={{ fontSize: 12 }}
                        >
                          {isProcessing ? 'Salvando...' : 'Entregue'}
                        </Button>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
        {dispatches.length > rowsPerPage && (
          <TablePagination
            component="div"
            count={dispatches.length}
            page={page}
            rowsPerPage={rowsPerPage}
            rowsPerPageOptions={[25, 50, 100]}
            onPageChange={(_, p) => setPage(p)}
            onRowsPerPageChange={e => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }}
            labelRowsPerPage="Por página:"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
          />
        )}
      </TableContainer>

      {createOpen && (
        <CreateDispatchModal
          open
          tenantCode={tenantCode}
          onCreated={handleCreated}
          onClose={() => setCreateOpen(false)}
        />
      )}

      {conferenceDispatch && orderMap.get(conferenceDispatch.shipmentOrderId) && (
        <ConferenceModal
          dispatch={conferenceDispatch}
          order={orderMap.get(conferenceDispatch.shipmentOrderId)!}
          tenantCode={tenantCode}
          onConferenced={handleConferenced}
          onClose={() => setConferenceDispatch(null)}
        />
      )}

      {printDispatch && orderMap.get(printDispatch.shipmentOrderId) && (
        <DispatchPrintView
          dispatch={printDispatch}
          order={orderMap.get(printDispatch.shipmentOrderId)!}
          vehicle={printDispatch.vehicleId ? vehicleMap.get(printDispatch.vehicleId) : undefined}
          driver={printDispatch.driverPersonId ? personMap.get(printDispatch.driverPersonId) : undefined}
          carrier={carrierMap.get(printDispatch.carrierId)}
          onClose={() => setPrintDispatch(null)}
        />
      )}

      {damdfeDispatch && orderMap.get(damdfeDispatch.shipmentOrderId) && (
        <DamdfePrintView
          dispatch={damdfeDispatch}
          order={orderMap.get(damdfeDispatch.shipmentOrderId)!}
          vehicle={damdfeDispatch.vehicleId ? vehicleMap.get(damdfeDispatch.vehicleId) : undefined}
          driver={damdfeDispatch.driverPersonId ? personMap.get(damdfeDispatch.driverPersonId) : undefined}
          onClose={() => setDamdfeDispatch(null)}
        />
      )}

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
        onConfirm={handleConfirm}
        onCancel={() => setConfirm(null)}
      />

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
