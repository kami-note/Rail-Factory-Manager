import React, { useEffect, useState } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, Button, IconButton, Stack,
} from '@mui/material';
import { X } from 'lucide-react';
import { createShipmentOrder } from '../api/logistics';
import type { ShipmentOrder } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { open: boolean; tenantCode: string; onCreated: (o: ShipmentOrder) => void; onClose: () => void };

export function CreateShipmentOrderModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setNotes(''); setSaving(false); setError(null); }
  }, [open]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    setSaving(true); setError(null);
    try {
      const order = await createShipmentOrder(tenantCode, { notes: notes.trim() || undefined });
      onCreated(order);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar ordem de expedição.'));
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="xs" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Nova Ordem de Expedição
          <IconButton onClick={handleClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <div style={{ color: 'red', fontSize: 13 }}>{error}</div>}
            <TextField label="Observações (opcional)" value={notes} onChange={e => setNotes(e.target.value)} fullWidth multiline rows={3} size="small" />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {saving ? 'Criando...' : 'Criar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
