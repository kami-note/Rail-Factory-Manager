import React, { useState } from 'react';
import {
  Alert, Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, MenuItem, Stack, TextField, Typography,
} from '@mui/material';
import { UserCheck, X } from 'lucide-react';
import { assignDriver } from '../api/fleet';
import { usePeople } from '../../hr/hooks/usePeople';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { DriverAssignment, Vehicle } from '../types';

type Props = {
  vehicle: Vehicle;
  tenantCode: string;
  onAssigned: (a: DriverAssignment) => void;
  onClose: () => void;
};

export function AssignDriverModal({ vehicle, tenantCode, onAssigned, onClose }: Props) {
  const { data: people } = usePeople(tenantCode);
  const drivers = people?.filter(p => p.type.key === 'driver') ?? [];

  const today = new Date().toISOString().slice(0, 10);
  const [driverPersonId, setDriverPersonId] = useState('');
  const [startDate, setStartDate] = useState(today);
  const [endDate, setEndDate] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = !!driverPersonId && !!startDate && !saving;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    setSaving(true);
    setError(null);
    try {
      const result = await assignDriver(tenantCode, vehicle.id, {
        driverPersonId,
        startDate,
        endDate: endDate || undefined,
        notes: notes || undefined,
      });
      onAssigned(result);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Falha ao alocar motorista.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open onClose={() => !saving && onClose()} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <UserCheck size={20} />
          <span>Alocar Motorista</span>
        </Stack>
        <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            Veículo: <strong style={{ fontFamily: 'monospace' }}>{vehicle.plate}</strong>
          </Typography>

          <TextField
            select
            label="Motorista"
            value={driverPersonId}
            onChange={e => setDriverPersonId(e.target.value)}
            size="small"
            required
            disabled={saving}
          >
            {drivers.length === 0 && (
              <MenuItem value="" disabled>Nenhum motorista cadastrado</MenuItem>
            )}
            {drivers.map(d => (
              <MenuItem key={d.id} value={d.id}>{d.name}</MenuItem>
            ))}
          </TextField>

          <TextField
            label="Início da alocação"
            type="date"
            value={startDate}
            onChange={e => setStartDate(e.target.value)}
            size="small"
            required
            disabled={saving}
            sx={{ '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
          />

          <TextField
            label="Fim da alocação (opcional)"
            type="date"
            value={endDate}
            onChange={e => setEndDate(e.target.value)}
            size="small"
            disabled={saving}
            helperText="Deixe em branco para alocação em aberto"
            sx={{ '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
          />

          <TextField
            label="Observações (opcional)"
            value={notes}
            onChange={e => setNotes(e.target.value)}
            size="small"
            multiline
            rows={2}
            disabled={saving}
          />

          {error && <Alert severity="error">{error}</Alert>}
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={onClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          disabled={!canSubmit}
          onClick={handleSubmit}
          startIcon={saving ? <CircularProgress size={14} color="inherit" /> : <UserCheck size={16} />}
        >
          {saving ? 'Salvando...' : 'Alocar'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
