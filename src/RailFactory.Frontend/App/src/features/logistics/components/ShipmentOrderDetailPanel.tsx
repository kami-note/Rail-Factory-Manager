import React from 'react';
import {
  Box, Button, Chip, Divider, Drawer, IconButton,
  Stack, Step, StepLabel, Stepper, Table, TableBody, TableCell,
  TableHead, TableRow, Tooltip, Typography,
} from '@mui/material';
import { ArrowRight, PackageCheck, PackageOpen, Tag, Truck, X, XCircle } from 'lucide-react';
import type { Dispatch, ShipmentOrder, ShipmentOrderStatus } from '../types';
import { CurrencyFormatter, RelativeDateFormatter } from '../../../shared/lib/utils/formatters';

const STATUS_LABELS: Record<ShipmentOrderStatus, string> = {
  Draft: 'Rascunho', Picking: 'Separação', Packing: 'Embalagem',
  ReadyToShip: 'Pronto p/ Despacho', Shipped: 'Despachado', Cancelled: 'Cancelado',
};

const STATUS_COLOR: Record<ShipmentOrderStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Draft: 'default', Picking: 'info', Packing: 'warning',
  ReadyToShip: 'success', Shipped: 'success', Cancelled: 'error',
};

const DISPATCH_STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendente', InTransit: 'Em trânsito', Delivered: 'Entregue', Returned: 'Devolvido',
};

const FLOW_STEPS: { status: ShipmentOrderStatus; label: string }[] = [
  { status: 'Draft', label: 'Rascunho' },
  { status: 'Picking', label: 'Separação' },
  { status: 'Packing', label: 'Embalagem' },
  { status: 'ReadyToShip', label: 'Pronto' },
  { status: 'Shipped', label: 'Despachado' },
];

type Props = {
  order: ShipmentOrder;
  dispatch?: Dispatch;
  carrierName?: string;
  onClose: () => void;
  onTransition: (id: string, action: string, label: string) => void;
};

export function ShipmentOrderDetailPanel({ order, dispatch, carrierName, onClose, onTransition }: Props) {
  const totalWeight = order.items.reduce((s, i) => s + i.weightKg * i.quantity, 0);
  const totalVolume = order.items.reduce((s, i) => s + i.volumeCbm * i.quantity, 0);
  const totalValue = order.items.every(i => !i.unitValue) ? null
    : order.items.reduce((s, i) => s + (i.unitValue ?? 0) * i.quantity, 0);

  const isCancelled = order.status === 'Cancelled';
  const activeStep = isCancelled ? -1 : FLOW_STEPS.findIndex(s => s.status === order.status);

  return (
    <Drawer anchor="right" open onClose={onClose} sx={{ '& .MuiDrawer-paper': { width: { xs: '100%', sm: 500 } } }}>
      <Stack sx={{ height: '100%' }}>

        {/* Header */}
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', px: 2.5, py: 2, borderBottom: 1, borderColor: 'divider' }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <PackageCheck size={20} />
            <Typography variant="h6" sx={{ fontWeight: 800, fontFamily: 'monospace' }}>
              {order.orderNumber}
            </Typography>
            <Chip label={STATUS_LABELS[order.status]} color={STATUS_COLOR[order.status]} size="small" />
          </Stack>
          <IconButton onClick={onClose} size="small"><X size={18} /></IconButton>
        </Stack>

        {/* Timeline de progresso */}
        {isCancelled ? (
          <Box sx={{ px: 2.5, py: 1.5, borderBottom: 1, borderColor: 'divider', bgcolor: 'error.50' }}>
            <Typography variant="caption" color="error.main" sx={{ fontWeight: 600 }}>
              Ordem cancelada
            </Typography>
          </Box>
        ) : (
          <Box sx={{ px: 2, py: 2, borderBottom: 1, borderColor: 'divider', bgcolor: 'action.hover' }}>
            <Stepper activeStep={activeStep} alternativeLabel sx={{ '& .MuiStepLabel-label': { fontSize: 10 } }}>
              {FLOW_STEPS.map((step, idx) => (
                <Step key={step.status} completed={idx < activeStep}>
                  <StepLabel>{step.label}</StepLabel>
                </Step>
              ))}
            </Stepper>
          </Box>
        )}

        {/* Body rolável */}
        <Box sx={{ flex: 1, overflowY: 'auto', px: 2.5, py: 2 }}>
          <Stack spacing={3}>

            {/* Despacho vinculado */}
            {dispatch && (
              <>
                <Stack spacing={1}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                    Despacho
                  </Typography>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                    {carrierName && (
                      <Chip icon={<Truck size={12} />} label={carrierName} size="small" variant="outlined" />
                    )}
                    <Chip
                      label={DISPATCH_STATUS_LABELS[dispatch.status] ?? dispatch.status}
                      size="small"
                      color={dispatch.status === 'Delivered' ? 'success' : dispatch.status === 'InTransit' ? 'info' : 'default'}
                    />
                  </Stack>
                  {dispatch.trackingCode && (
                    <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                      <Tag size={12} color="var(--mui-palette-text-secondary, #757575)" />
                      <Typography variant="caption" sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>
                        {dispatch.trackingCode}
                      </Typography>
                    </Stack>
                  )}
                  {dispatch.freightValueBrl > 0 && (
                    <Typography variant="caption" color="text.secondary">
                      Frete: <strong>{CurrencyFormatter.format(dispatch.freightValueBrl)}</strong>
                    </Typography>
                  )}
                  {dispatch.dispatchedAt && (
                    <Typography variant="caption" color="text.secondary">
                      Despachado em {RelativeDateFormatter.format(dispatch.dispatchedAt)}
                    </Typography>
                  )}
                </Stack>
                <Divider />
              </>
            )}

            {/* Destinatário */}
            {(order.recipientName || order.recipientCnpj) && (
              <>
                <Stack spacing={0.5}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                    Destinatário
                  </Typography>
                  {order.recipientName && (
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{order.recipientName}</Typography>
                  )}
                  {order.recipientCnpj && (
                    <Typography variant="caption" color="text.secondary">{order.recipientCnpj}</Typography>
                  )}
                  {order.recipientStreet && (
                    <Typography variant="caption" color="text.secondary">
                      {order.recipientStreet}{order.recipientNumber ? `, ${order.recipientNumber}` : ''}
                      {order.recipientDistrict ? ` — ${order.recipientDistrict}` : ''}
                    </Typography>
                  )}
                  {order.recipientCity && (
                    <Typography variant="caption" color="text.secondary">
                      {order.recipientCity}{order.recipientState ? `/${order.recipientState}` : ''}
                      {order.recipientZipCode ? ` — CEP ${order.recipientZipCode}` : ''}
                    </Typography>
                  )}
                </Stack>
                <Divider />
              </>
            )}

            {/* Itens */}
            <Stack spacing={1}>
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                  Itens ({order.items.length})
                </Typography>
                <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                  <Typography variant="caption" color="text.secondary">
                    {totalWeight.toFixed(2)} kg
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {totalVolume.toFixed(3)} m³
                  </Typography>
                  {totalValue !== null && (
                    <Typography variant="caption" sx={{ fontWeight: 700, color: 'text.primary' }}>
                      {CurrencyFormatter.format(totalValue)}
                    </Typography>
                  )}
                </Stack>
              </Stack>

              {order.items.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                  Nenhum item cadastrado.
                </Typography>
              ) : (
                <Table size="small" sx={{ '& td, & th': { px: 1, py: 0.75 } }}>
                  <TableHead>
                    <TableRow sx={{ '& th': { fontWeight: 700, fontSize: 11, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: 0.3 } }}>
                      <TableCell>Código</TableCell>
                      <TableCell align="right">Qtd</TableCell>
                      <TableCell>UM</TableCell>
                      <TableCell align="right">Peso (kg)</TableCell>
                      <TableCell align="right">Valor</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {order.items.map(item => (
                      <TableRow key={item.id} hover>
                        <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: 13 }}>
                          {item.materialCode}
                        </TableCell>
                        <TableCell align="right">
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>{item.quantity}</Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" color="text.secondary">{item.unitOfMeasure}</Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography variant="caption" color="text.secondary">
                            {(item.weightKg * item.quantity).toFixed(2)}
                          </Typography>
                        </TableCell>
                        <TableCell align="right">
                          <Typography variant="caption" color="text.secondary">
                            {item.unitValue ? CurrencyFormatter.format(item.unitValue * item.quantity) : '—'}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </Stack>

            {/* Metadados */}
            <Divider />
            <Stack spacing={0.5}>
              <Typography variant="caption" color="text.secondary">
                Criado em {RelativeDateFormatter.format(order.createdAt, false)}
              </Typography>
              {order.productionOrderRef && (
                <Typography variant="caption" color="text.secondary">
                  OP: <strong>{order.productionOrderRef}</strong>
                </Typography>
              )}
              {order.notes && (
                <Typography variant="caption" color="text.secondary">
                  Obs: {order.notes}
                </Typography>
              )}
            </Stack>

          </Stack>
        </Box>

        {/* Ações fixas no rodapé */}
        {['Draft', 'Picking', 'Packing'].includes(order.status) && (
          <Stack spacing={1} sx={{ px: 2.5, py: 2, borderTop: 1, borderColor: 'divider' }}>
            {order.status === 'Draft' && (
              <Button
                fullWidth variant="contained" color="info"
                startIcon={<PackageOpen size={16} />}
                onClick={() => { onTransition(order.id, 'start-picking', 'Iniciar Separação'); onClose(); }}
              >
                Iniciar Separação
              </Button>
            )}
            {order.status === 'Picking' && (
              <Button
                fullWidth variant="contained" color="warning"
                startIcon={<PackageCheck size={16} />}
                onClick={() => { onTransition(order.id, 'start-packing', 'Iniciar Embalagem'); onClose(); }}
              >
                Iniciar Embalagem
              </Button>
            )}
            {order.status === 'Packing' && (
              <Button
                fullWidth variant="contained" color="success"
                startIcon={<ArrowRight size={16} />}
                onClick={() => { onTransition(order.id, 'ready-to-ship', 'Pronto p/ Despacho'); onClose(); }}
              >
                Marcar Pronto p/ Despacho
              </Button>
            )}
            <Tooltip title="Cancelar ordem">
              <Button
                fullWidth variant="outlined" color="error" size="small"
                startIcon={<XCircle size={14} />}
                onClick={() => { onTransition(order.id, 'cancel', 'Cancelar'); onClose(); }}
              >
                Cancelar Ordem
              </Button>
            </Tooltip>
          </Stack>
        )}

        {order.status === 'ReadyToShip' && (
          <Stack sx={{ px: 2.5, py: 2, borderTop: 1, borderColor: 'divider' }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Truck size={16} color="var(--mui-palette-success-main, #2e7d32)" />
              <Typography variant="body2" sx={{ fontWeight: 700, color: 'success.main' }}>
                Pronta para despacho — crie o despacho na aba Despachos.
              </Typography>
            </Stack>
          </Stack>
        )}

      </Stack>
    </Drawer>
  );
}
