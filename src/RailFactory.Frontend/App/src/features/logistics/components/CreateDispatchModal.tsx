import React, { useEffect, useState } from 'react';
import {
  Alert, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, MenuItem, Select, Stack, Typography,
} from '@mui/material';
import { Button } from '@mui/material';
import { X } from 'lucide-react';
import { createDispatch } from '../api/logistics';
import { useCarriers } from '../hooks/useCarriers';
import { useShipmentOrders } from '../hooks/useShipmentOrders';
import type { Dispatch } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = {
  open: boolean;
  tenantCode: string;
  onCreated: (dispatch: Dispatch) => void;
  onClose: () => void;
};

export function CreateDispatchModal({ open, tenantCode, onCreated, onClose }: Props) {
  const { data: orders, loading: ordersLoading } = useShipmentOrders(tenantCode);
  const { data: carriers, loading: carriersLoading } = useCarriers(tenantCode);
  const [shipmentOrderId, setShipmentOrderId] = useState('');
  const [carrierId, setCarrierId] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setShipmentOrderId(''); setCarrierId(''); setSaving(false); setError(null); }
  }, [open]);

  const readyOrders = orders?.filter(o => o.status === 'ReadyToShip') ?? [];
  const activeCarriers = carriers?.filter(c => c.status === 'Active') ?? [];
  const loading = ordersLoading || carriersLoading;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving || !shipmentOrderId || !carrierId) return;
    setSaving(true); setError(null);
    try {
      const dispatch = await createDispatch(tenantCode, { shipmentOrderId, carrierId });
      onCreated(dispatch);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar despacho.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="xs" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Novo Despacho
          <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>

        <DialogContent>
          {loading
            ? <Stack  sx={{py: 3, alignItems: 'center'}}><CircularProgress size={28} /></Stack>
            : (
              <Stack spacing={2} sx={{ mt: 1 }}>
                {error && <Alert severity="error">{error}</Alert>}

                {readyOrders.length === 0
                  ? <Alert severity="info">Nenhuma ordem com status "Pronto p/ Despacho".</Alert>
                  : (
                    <FormControl fullWidth size="small" required>
                      <InputLabel>Ordem de Expedição</InputLabel>
                      <Select value={shipmentOrderId} label="Ordem de Expedição" onChange={e => setShipmentOrderId(e.target.value)}>
                        {readyOrders.map(o => (
                          <MenuItem key={o.id} value={o.id}>
                            <Stack>
                              <Typography variant="body2" fontWeight={700} fontFamily="monospace">{o.orderNumber}</Typography>
                              {o.recipientName && <Typography variant="caption" color="text.secondary">{o.recipientName}</Typography>}
                            </Stack>
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}

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
            disabled={saving || loading || !shipmentOrderId || !carrierId}
          >
            {saving ? 'Criando...' : 'Criar Despacho'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
