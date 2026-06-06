import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, FormControl, InputLabel, MenuItem,
  Paper, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, Typography,
} from '@mui/material';
import { Plus, X } from 'lucide-react';
import { listFuelingRecords, recordFueling, type FuelingRecord } from '../api/fleet-maintenance';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { Vehicle } from '../types';

type Props = {
  tenantCode: string;
  vehicleId?: string;
  vehicles?: Vehicle[];
};

export function FuelingContent({ tenantCode, vehicleId: vehicleIdProp, vehicles }: Props) {
  const fleetMode = !vehicleIdProp;

  const [records, setRecords] = useState<FuelingRecord[]>([]);
  const [filterVehicleId, setFilterVehicleId] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [formVehicleId, setFormVehicleId] = useState('');
  const [formDate, setFormDate] = useState('');
  const [formLiters, setFormLiters] = useState('');
  const [formPrice, setFormPrice] = useState('');
  const [formOdometer, setFormOdometer] = useState('');
  const [formSupplier, setFormSupplier] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (fleetMode) {
      if (!vehicles || vehicles.length === 0) return;
      setLoading(true); setLoadError(null);
      Promise.all(vehicles.map(v => listFuelingRecords(tenantCode, v.id)))
        .then(results => {
          const combined = results.flat().sort((a, b) => b.date.localeCompare(a.date));
          setRecords(combined);
        })
        .catch(err => setLoadError(toUiErrorMessage(err, 'Erro ao carregar abastecimentos.')))
        .finally(() => setLoading(false));
    } else {
      setLoading(true); setLoadError(null);
      listFuelingRecords(tenantCode, vehicleIdProp!)
        .then(setRecords)
        .catch(err => setLoadError(toUiErrorMessage(err, 'Erro ao carregar abastecimentos.')))
        .finally(() => setLoading(false));
    }
  }, [tenantCode, vehicleIdProp, vehicles, fleetMode]);

  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const displayRecords = records.filter(r => {
    if (fleetMode && filterVehicleId && r.vehicleId !== filterVehicleId) return false;
    if (fromDate && r.date < fromDate) return false;
    if (toDate && r.date > toDate) return false;
    return true;
  });

  const plateLookup = (vehicleId: string) =>
    vehicles?.find(v => v.id === vehicleId)?.plate ?? vehicleId.slice(0, 8);

  const totalLiters = displayRecords.reduce((s, r) => s + r.litersSupplied, 0);
  const totalCost = displayRecords.reduce((s, r) => s + r.totalBrl, 0);

  const resetForm = () => {
    setFormDate(''); setFormLiters(''); setFormPrice('');
    setFormOdometer(''); setFormSupplier(''); setFormVehicleId('');
  };

  const handleRecord = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetId = fleetMode ? formVehicleId : vehicleIdProp!;
    if (!targetId || saving) return;
    setSaving(true); setMutationError(null);
    try {
      const record = await recordFueling(tenantCode, targetId, {
        date: formDate, litersSupplied: parseFloat(formLiters),
        pricePerLiter: parseFloat(formPrice),
        odometer: formOdometer ? parseInt(formOdometer) : undefined,
        supplier: formSupplier.trim() || undefined,
      });
      setRecords(prev => [record, ...prev]);
      setCreateOpen(false); resetForm();
      setSuccess('Abastecimento registrado.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao registrar abastecimento.'));
    } finally { setSaving(false); }
  };

  return (
    <Box>
      {/* Fleet-mode toolbar */}
      {fleetMode && (
        <Stack spacing={1.5} sx={{ mb: 2 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          <FormControl size="small" sx={{ minWidth: 220 }}>
            <InputLabel>Veículo</InputLabel>
            <Select value={filterVehicleId} label="Veículo" onChange={e => setFilterVehicleId(e.target.value)}>
              <MenuItem value="">Todos os veículos</MenuItem>
              {(vehicles ?? []).map(v => (
                <MenuItem key={v.id} value={v.id}>{v.plate} — {v.type.label}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button variant="contained" size="small" startIcon={<Plus size={15} />} onClick={() => setCreateOpen(true)}>
            Registrar
          </Button>
        </Stack>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>PERÍODO:</Typography>
          <TextField
            label="De"
            type="date"
            value={fromDate}
            onChange={e => setFromDate(e.target.value)}
            size="small"
            sx={{ width: 150, '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
          />
          <TextField
            label="Até"
            type="date"
            value={toDate}
            onChange={e => setToDate(e.target.value)}
            size="small"
            sx={{ width: 150, '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }}
          />
        </Stack>
        </Stack>
      )}

      {/* Drawer-mode button */}
      {!fleetMode && (
        <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <Button variant="contained" size="small" startIcon={<Plus size={15} />} onClick={() => setCreateOpen(true)}>
            Registrar Abastecimento
          </Button>
        </Box>
      )}

      {loadError && <Alert severity="error" sx={{ mb: 2 }}>{loadError}</Alert>}

      {loading ? <CircularProgress size={24} /> : (
        <>
          {displayRecords.length > 0 && (
            <Stack direction="row" spacing={3} sx={{ mb: 1.5 }}>
              <Typography variant="caption" color="text.secondary">
                Total: <strong>{totalLiters.toFixed(1)} L</strong>
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Custo total: <strong>R$ {totalCost.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}</strong>
              </Typography>
            </Stack>
          )}
          <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  {fleetMode && <TableCell sx={{ fontWeight: 700 }}>Veículo</TableCell>}
                  <TableCell>Data</TableCell>
                  <TableCell align="right">Litros</TableCell>
                  <TableCell align="right">R$/L</TableCell>
                  <TableCell align="right">Total (R$)</TableCell>
                  <TableCell align="right">Odômetro</TableCell>
                  <TableCell>Fornecedor</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {displayRecords.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={fleetMode ? 7 : 6} align="center" sx={{ color: 'text.secondary', py: 3 }}>
                      Nenhum abastecimento registrado.
                    </TableCell>
                  </TableRow>
                ) : displayRecords.map(r => (
                  <TableRow key={r.id} hover>
                    {fleetMode && (
                      <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace', fontSize: 12 }}>
                        {plateLookup(r.vehicleId)}
                      </TableCell>
                    )}
                    <TableCell sx={{ fontSize: 12 }}>{r.date}</TableCell>
                    <TableCell align="right">{r.litersSupplied.toFixed(1)}</TableCell>
                    <TableCell align="right" sx={{ fontSize: 12, color: 'text.secondary' }}>{r.pricePerLiter.toFixed(3)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700 }}>{r.totalBrl.toFixed(2)}</TableCell>
                    <TableCell align="right" sx={{ fontSize: 12, color: 'text.secondary' }}>{r.odometer ? `${r.odometer.toLocaleString()} km` : '—'}</TableCell>
                    <TableCell sx={{ fontSize: 12 }}>{r.supplier ?? '—'}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}

      <Dialog open={createOpen} onClose={() => !saving && setCreateOpen(false)} maxWidth="xs" fullWidth>
        <form onSubmit={handleRecord}>
          <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
            Registrar Abastecimento
            <IconButton onClick={() => !saving && setCreateOpen(false)} size="small"><X size={18} /></IconButton>
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {fleetMode && (
                <FormControl fullWidth size="small" required>
                  <InputLabel>Veículo</InputLabel>
                  <Select value={formVehicleId} label="Veículo" onChange={e => setFormVehicleId(e.target.value)}>
                    {(vehicles ?? []).map(v => (
                      <MenuItem key={v.id} value={v.id}>{v.plate} — {v.type.label}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
              <TextField label="Data" value={formDate} onChange={e => setFormDate(e.target.value)} required fullWidth size="small" type="date" sx={{ '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }} />
              <TextField label="Litros abastecidos" value={formLiters} onChange={e => setFormLiters(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.001' } }} />
              <TextField label="Preço por litro (R$)" value={formPrice} onChange={e => setFormPrice(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.0001' } }} />
              <TextField label="Odômetro (km)" value={formOdometer} onChange={e => setFormOdometer(e.target.value)} fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0 } }} />
              <TextField label="Fornecedor" value={formSupplier} onChange={e => setFormSupplier(e.target.value)} fullWidth size="small" />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => !saving && setCreateOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" variant="contained"
              disabled={!formDate || !formLiters || !formPrice || (fleetMode && !formVehicleId) || saving}>
              {saving ? 'Registrando...' : 'Registrar'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
