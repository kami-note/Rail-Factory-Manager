import React, { useState } from 'react';
import {
  Alert, Checkbox, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Divider, FormControlLabel, IconButton, LinearProgress, Stack, Typography,
} from '@mui/material';
import { Button } from '@mui/material';
import { ClipboardCheck, X } from 'lucide-react';
import { transitionDispatch } from '../api/logistics';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { Dispatch, ShipmentOrder } from '../types';

type Props = {
  dispatch: Dispatch;
  order: ShipmentOrder;
  tenantCode: string;
  onConferenced: () => void;
  onClose: () => void;
};

export function ConferenceModal({ dispatch, order, tenantCode, onConferenced, onClose }: Props) {
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const toggle = (id: string) =>
    setChecked(prev => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });

  const allChecked = order.items.length > 0 && checked.size === order.items.length;
  const progress = order.items.length > 0 ? (checked.size / order.items.length) * 100 : 0;

  const handleConfirm = async () => {
    if (!allChecked || saving) return;
    setSaving(true);
    setError(null);
    try {
      await transitionDispatch(tenantCode, dispatch.id, 'conference');
      onConferenced();
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao confirmar conferência.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open onClose={() => !saving && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <ClipboardCheck size={20} />
          <span>Conferência do Despacho</span>
        </Stack>
        <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
      </DialogTitle>

      <DialogContent sx={{ pb: 0 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Typography variant="body2" color="text.secondary">Despacho</Typography>
            <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
              {dispatch.trackingCode}
            </Typography>
            <Typography variant="body2" color="text.secondary">·</Typography>
            <Typography variant="body2" color="text.secondary">Ordem</Typography>
            <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
              {order.orderNumber}
            </Typography>
          </Stack>

          {order.recipientName && (
            <Typography variant="body2" color="text.secondary">
              Destinatário: <strong>{order.recipientName}</strong>
            </Typography>
          )}

          <Divider />

          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              Itens a conferir
            </Typography>
            <Chip
              size="small"
              label={`${checked.size} / ${order.items.length}`}
              color={allChecked ? 'success' : 'default'}
            />
          </Stack>

          <LinearProgress
            variant="determinate"
            value={progress}
            color={allChecked ? 'success' : 'primary'}
            sx={{ borderRadius: 1, height: 6 }}
          />

          {order.items.length === 0 && (
            <Alert severity="warning">Esta ordem não tem itens cadastrados.</Alert>
          )}

          <Stack spacing={0.5}>
            {order.items.map(item => (
              <Stack
                key={item.id}
                direction="row"
                sx={{
                  alignItems: 'center',
                  px: 1.5, py: 0.75,
                  borderRadius: 1,
                  bgcolor: checked.has(item.id) ? 'success.50' : 'action.hover',
                  border: 1,
                  borderColor: checked.has(item.id) ? 'success.light' : 'transparent',
                  transition: 'all 0.15s',
                }}
              >
                <FormControlLabel
                  sx={{ flex: 1, m: 0 }}
                  control={
                    <Checkbox
                      checked={checked.has(item.id)}
                      onChange={() => toggle(item.id)}
                      disabled={saving}
                      size="small"
                      color="success"
                    />
                  }
                  label={
                    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'baseline' }}>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 700 }}>
                        {item.materialCode}
                      </Typography>
                      <Typography variant="body2">
                        {item.quantity} {item.unitOfMeasure}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {item.weightKg} kg
                      </Typography>
                    </Stack>
                  }
                />
              </Stack>
            ))}
          </Stack>

          {error && <Alert severity="error">{error}</Alert>}

          {!allChecked && order.items.length > 0 && (
            <Typography variant="caption" color="text.secondary" sx={{ pb: 1 }}>
              Marque todos os {order.items.length} itens para liberar a confirmação.
            </Typography>
          )}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          color="success"
          disabled={!allChecked || saving}
          onClick={handleConfirm}
          startIcon={saving ? <CircularProgress size={14} color="inherit" /> : <ClipboardCheck size={16} />}
        >
          {saving ? 'Confirmando...' : 'Confirmar Conferência'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
