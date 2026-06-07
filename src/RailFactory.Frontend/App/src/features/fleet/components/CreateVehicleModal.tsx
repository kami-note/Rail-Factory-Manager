import React, { useEffect, useState } from 'react';
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import { Truck, X } from 'lucide-react';
import { InlineError } from '../../../shared/components/common/InlineError';
import { createVehicle } from '../api/fleet';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { Vehicle } from '../types';

const VEHICLE_TYPE_OPTIONS = [
  { value: 'Car',        label: 'Carro' },
  { value: 'Truck',      label: 'Caminhão' },
  { value: 'Van',        label: 'Van' },
  { value: 'Motorcycle', label: 'Moto' },
];

type Props = {
  open: boolean;
  tenantCode: string;
  onCreated: (vehicle: Vehicle) => void;
  onClose: () => void;
};

export function CreateVehicleModal({ open, tenantCode, onCreated, onClose }: Props) {
  const [plate, setPlate] = useState('');
  const [chassis, setChassis] = useState('');
  const [renavam, setRenavam] = useState('');
  const [rntrc, setRntrc] = useState('');
  const [type, setType] = useState('Truck');
  const [maxWeightKg, setMaxWeightKg] = useState('');
  const [maxVolumeCbm, setMaxVolumeCbm] = useState('');
  const [licenseExpiry, setLicenseExpiry] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setPlate(''); setChassis(''); setRenavam(''); setRntrc(''); setType('Truck');
      setMaxWeightKg(''); setMaxVolumeCbm(''); setLicenseExpiry('');
      setError(null); setSaving(false);
    }
  }, [open]);

  const isValid =
    plate.trim().length > 0 &&
    chassis.trim().length > 0 &&
    renavam.trim().length > 0 &&
    maxWeightKg !== '' &&
    maxVolumeCbm !== '' &&
    licenseExpiry !== '';

  const handleSubmit = async () => {
    if (!isValid) return;
    setSaving(true);
    setError(null);
    try {
      const vehicle = await createVehicle(tenantCode, {
        plate: plate.trim().toUpperCase(),
        chassis: chassis.trim().toUpperCase(),
        renavam: renavam.trim(),
        rntrc: rntrc.trim() || undefined,
        type,
        maxWeightKg: parseFloat(maxWeightKg),
        maxVolumeCbm: parseFloat(maxVolumeCbm),
        licenseExpiry,
      });
      onCreated(vehicle);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível cadastrar o veículo.'));
    } finally {
      setSaving(false);
    }
  };

  const handleClose = () => { if (!saving) onClose(); };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontWeight: 800 }}>
        Novo Veículo
        <IconButton size="small" onClick={handleClose} disabled={saving}>
          <X size={18} />
        </IconButton>
      </DialogTitle>

      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <InlineError message={error} marginBottom={0} />}

          <Stack direction="row" spacing={2}>
            <TextField
              label="Placa"
              size="small"
              sx={{ width: 130 }}
              value={plate}
              onChange={e => setPlate(e.target.value.toUpperCase())}
              placeholder="ABC-1234"
              slotProps={{ htmlInput: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
              autoFocus
            />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Tipo</InputLabel>
              <Select value={type} label="Tipo" onChange={e => setType(e.target.value)}>
                {VEHICLE_TYPE_OPTIONS.map(opt => (
                  <MenuItem key={opt.value} value={opt.value}>{opt.label}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="RENAVAM"
              size="small"
              sx={{ flexGrow: 1 }}
              value={renavam}
              onChange={e => setRenavam(e.target.value)}
              placeholder="00123456789"
              slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }}
            />
          </Stack>

          <Stack direction="row" spacing={2}>
            <TextField
              label="Chassi"
              size="small"
              sx={{ flexGrow: 1 }}
              value={chassis}
              onChange={e => setChassis(e.target.value.toUpperCase())}
              placeholder="9BWZZZ377VT004251"
              slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }}
            />
            <TextField
              label="RNTRC"
              size="small"
              sx={{ width: 140 }}
              value={rntrc}
              onChange={e => setRntrc(e.target.value)}
              placeholder="12345678"
              helperText="Opcional — para MDF-e"
              slotProps={{ htmlInput: { style: { fontFamily: 'monospace' } } }}
            />
          </Stack>

          <Stack direction="row" spacing={2}>
            <TextField
              label="Carga Máx. (kg)"
              size="small"
              sx={{ flexGrow: 1 }}
              type="number"
              value={maxWeightKg}
              onChange={e => setMaxWeightKg(e.target.value)}
              placeholder="5000"
            />
            <TextField
              label="Volume Máx. (m³)"
              size="small"
              sx={{ flexGrow: 1 }}
              type="number"
              value={maxVolumeCbm}
              onChange={e => setMaxVolumeCbm(e.target.value)}
              placeholder="20.5"
            />
            <TextField
              label="Vencimento CRLV"
              size="small"
              sx={{ flexGrow: 1 }}
              type="date"
              value={licenseExpiry}
              onChange={e => setLicenseExpiry(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Button onClick={handleClose} disabled={saving}>Cancelar</Button>
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !isValid}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : <Truck size={16} />}
          sx={{ fontWeight: 800 }}
        >
          Cadastrar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
