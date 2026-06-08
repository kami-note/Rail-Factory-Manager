import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  Divider, FormControl, IconButton, InputLabel, MenuItem, Select, Stack, Typography,
} from '@mui/material';
import { Button } from '@mui/material';
import { X } from 'lucide-react';
import { createDispatch } from '../api/logistics';
import { useCarriers } from '../hooks/useCarriers';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import { useVehicles } from '../../fleet/hooks/useVehicles';
import { useDriverAssignments } from '../../fleet/hooks/useDriverAssignments';
import { usePeople } from '../../hr/hooks/usePeople';
import type { Dispatch } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { Masks } from '../../../shared/lib/utils/masks';

type Props = {
  open: boolean;
  tenantCode: string;
  initialOrderId?: string;
  onCreated: (dispatch: Dispatch) => void;
  onClose: () => void;
};

export function CreateDispatchModal({ open, tenantCode, initialOrderId, onCreated, onClose }: Props) {
  const { data: orders, loading: ordersLoading } = useShipmentOrders(tenantCode);
  const { data: carriers, loading: carriersLoading } = useCarriers(tenantCode);
  const { data: vehicles, loading: vehiclesLoading } = useVehicles(tenantCode);
  const { data: people, loading: peopleLoading } = usePeople(tenantCode);

  const [shipmentOrderId, setShipmentOrderId] = useState(initialOrderId ?? '');
  const [carrierId, setCarrierId] = useState('');
  const [vehicleId, setVehicleId] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: assignments, loading: assignmentsLoading } = useDriverAssignments(tenantCode, vehicleId);

  const resolvedDriverId = useMemo(() => {
    if (!assignments?.length) return null;
    const today = new Date().toISOString().slice(0, 10);
    return assignments.find(a => a.startDate <= today && (a.endDate == null || a.endDate >= today))
      ?.driverPersonId ?? null;
  }, [assignments]);

  const personMap = useMemo(() => new Map(people?.map(p => [p.id, p]) ?? []), [people]);
  const resolvedDriver = resolvedDriverId ? personMap.get(resolvedDriverId) : undefined;

  useEffect(() => {
    if (!open) {
      setShipmentOrderId(initialOrderId ?? ''); setCarrierId(''); setVehicleId('');
      setSaving(false); setError(null);
    }
  }, [open, initialOrderId]);

  const readyOrders = orders?.filter(o => o.status === 'ReadyToShip') ?? [];
  const activeCarriers = carriers?.filter(c => c.status === 'Active') ?? [];
  const activeVehicles = vehicles?.filter(v => v.status.key === 'active') ?? [];
  const loading = ordersLoading || carriersLoading || vehiclesLoading || peopleLoading;

  const selectedOrder = orders?.find(o => o.id === shipmentOrderId);

  const noDriverAssigned = vehicleId && !assignmentsLoading && !resolvedDriverId;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving || !shipmentOrderId || !carrierId || !vehicleId || !resolvedDriverId) return;
    setSaving(true); setError(null);
    try {
      const selectedVehicle = vehicles?.find(v => v.id === vehicleId);
      const dispatch = await createDispatch(tenantCode, {
        shipmentOrderId,
        carrierId,
        vehicleId,
        driverPersonId: resolvedDriverId,
        vehiclePlate: selectedVehicle?.plate,
        vehicleRntrc: selectedVehicle?.rntrc,
        driverCpf: resolvedDriver?.documentNumber,
        driverName: resolvedDriver?.name,
      });
      onCreated(dispatch);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar despacho.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Novo Despacho
          <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>

        <DialogContent>
          {loading
            ? <Stack sx={{ py: 3, alignItems: 'center' }}><CircularProgress size={28} /></Stack>
            : (
              <Stack spacing={2} sx={{ mt: 1 }}>
                {error && <Alert severity="error">{error}</Alert>}

                {/* Ordem de Expedição */}
                {initialOrderId && selectedOrder ? (
                  <Stack spacing={0.5} sx={{ px: 1.5, py: 1, bgcolor: 'action.hover', borderRadius: 1, border: 1, borderColor: 'divider' }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                      Ordem de Expedição
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{selectedOrder.orderNumber}</Typography>
                    {selectedOrder.recipientName && (
                      <Typography variant="caption" color="text.secondary">{selectedOrder.recipientName}</Typography>
                    )}
                  </Stack>
                ) : readyOrders.length === 0 ? (
                  <Alert severity="info">Nenhuma ordem com status "Pronto p/ Despacho".</Alert>
                ) : (
                  <FormControl fullWidth size="small" required>
                    <InputLabel>Ordem de Expedição</InputLabel>
                    <Select value={shipmentOrderId} label="Ordem de Expedição" onChange={e => setShipmentOrderId(e.target.value)}>
                      {readyOrders.map(o => (
                        <MenuItem key={o.id} value={o.id}>
                          <Stack>
                            <Typography variant="body2" sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{o.orderNumber}</Typography>
                            {o.recipientName && <Typography variant="caption" color="text.secondary">{o.recipientName}</Typography>}
                          </Stack>
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}

                {/* Itens da ordem selecionada */}
                {selectedOrder && selectedOrder.items.length > 0 && (
                  <Stack spacing={0.5}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
                      Itens a despachar ({selectedOrder.items.length})
                    </Typography>
                    <Stack direction="row" sx={{ flexWrap: 'wrap', gap: 0.5 }}>
                      {selectedOrder.items.map(item => (
                        <Chip
                          key={item.id}
                          size="small"
                          label={`${item.materialCode} × ${item.quantity} ${item.unitOfMeasure}`}
                          variant="outlined"
                        />
                      ))}
                    </Stack>
                  </Stack>
                )}

                <Divider />

                {/* Transportadora */}
                <FormControl fullWidth size="small" required>
                  <InputLabel>Transportadora</InputLabel>
                  <Select value={carrierId} label="Transportadora" onChange={e => setCarrierId(e.target.value)}>
                    {activeCarriers.map(c => (
                      <MenuItem key={c.id} value={c.id}>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <Typography variant="body2">{c.name}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            R$ {c.ratePerKg.toFixed(2)}/kg
                          </Typography>
                        </Stack>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                {/* Veículo */}
                <FormControl fullWidth size="small" required>
                  <InputLabel>Veículo</InputLabel>
                  <Select
                    value={vehicleId}
                    label="Veículo"
                    onChange={e => setVehicleId(e.target.value)}
                  >
                    {activeVehicles.map(v => (
                      <MenuItem key={v.id} value={v.id}>
                        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                          <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{v.plate}</Typography>
                          <Typography variant="caption" color="text.secondary">{v.type.label}</Typography>
                        </Stack>
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                {/* Motorista — resolvido automaticamente da alocação do veículo */}
                {vehicleId && (
                  assignmentsLoading
                    ? <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <CircularProgress size={14} />
                        <Typography variant="caption" color="text.secondary">Buscando motorista...</Typography>
                      </Stack>
                    : noDriverAssigned
                      ? <Alert severity="warning">
                          Este veículo não tem motorista ativo atribuído. Atribua um no módulo <strong>Frota</strong> antes de criar o despacho.
                        </Alert>
                      : resolvedDriver && (
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', px: 1.5, py: 1, bgcolor: 'action.hover', borderRadius: 1 }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>{resolvedDriver.name}</Typography>
                            <Typography variant="caption" color="text.secondary">{Masks.cpfCnpj(resolvedDriver.documentNumber)}</Typography>
                            <Chip label="Motorista ativo" size="small" color="success" sx={{ ml: 'auto' }} />
                          </Stack>
                        )
                )}

                <Typography variant="caption" color="text.secondary">
                  O frete é calculado automaticamente com base no peso/volume dos itens e nas tarifas da transportadora.
                </Typography>
              </Stack>
            )}
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button
            type="submit"
            variant="contained"
            disabled={saving || loading || !shipmentOrderId || !carrierId || !vehicleId || !resolvedDriverId}
          >
            {saving ? 'Criando...' : 'Criar Despacho'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
