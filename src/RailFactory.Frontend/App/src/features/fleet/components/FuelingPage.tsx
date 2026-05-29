import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, FormControl, InputLabel, MenuItem,
  Paper, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Dialog, DialogTitle, DialogContent, DialogActions, IconButton,
} from '@mui/material';
import { Fuel, Plus, X } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { useVehicles } from '../hooks/useVehicles';
import { listFuelingRecords, recordFueling, type FuelingRecord } from '../api/fleet-maintenance';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };

export function FuelingPage({ tenantCode }: Props) {
  const { data: vehicles, loading: vehiclesLoading, error: vehiclesError } = useVehicles(tenantCode);
  const [vehicleId, setVehicleId] = useState('');
  const [records, setRecords] = useState<FuelingRecord[]>([]);
  const [loadingRecords, setLoadingRecords] = useState(false);
  const [recordsError, setRecordsError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  // form state
  const [formDate, setFormDate] = useState('');
  const [formLiters, setFormLiters] = useState('');
  const [formPrice, setFormPrice] = useState('');
  const [formOdometer, setFormOdometer] = useState('');
  const [formSupplier, setFormSupplier] = useState('');
  const [formNotes, setFormNotes] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!vehicleId) return;
    setLoadingRecords(true); setRecordsError(null);
    listFuelingRecords(tenantCode, vehicleId)
      .then(setRecords)
      .catch(err => setRecordsError(toUiErrorMessage(err, 'Erro ao carregar abastecimentos.')))
      .finally(() => setLoadingRecords(false));
  }, [tenantCode, vehicleId]);

  const resetForm = () => {
    setFormDate(''); setFormLiters(''); setFormPrice('');
    setFormOdometer(''); setFormSupplier(''); setFormNotes('');
  };

  const handleRecord = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vehicleId || saving) return;
    setSaving(true); setMutationError(null);
    try {
      const record = await recordFueling(tenantCode, vehicleId, {
        date: formDate, litersSupplied: parseFloat(formLiters),
        pricePerLiter: parseFloat(formPrice),
        odometer: formOdometer ? parseInt(formOdometer) : undefined,
        supplier: formSupplier.trim() || undefined,
        notes: formNotes.trim() || undefined,
      });
      setRecords(prev => [record, ...prev]);
      setCreateOpen(false);
      resetForm();
      setSuccess('Abastecimento registrado com sucesso.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao registrar abastecimento.'));
    } finally { setSaving(false); }
  };

  if (vehiclesLoading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (vehiclesError) return <PageError message={vehiclesError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Controle de Abastecimento" icon={<Fuel size={20} />} />

      <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center' }}>
        <FormControl size="small" sx={{ minWidth: 280 }}>
          <InputLabel>Selecionar Veículo</InputLabel>
          <Select value={vehicleId} label="Selecionar Veículo" onChange={e => setVehicleId(e.target.value)}>
            {(vehicles ?? []).map(v => (
              <MenuItem key={v.id} value={v.id}>{v.plate} — {v.type?.label ?? (v as any).type}</MenuItem>
            ))}
          </Select>
        </FormControl>
        {vehicleId && (
          <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
            Registrar Abastecimento
          </Button>
        )}
      </Stack>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      {vehicleId && (
        loadingRecords ? <CircularProgress /> :
        recordsError ? <Alert severity="error">{recordsError}</Alert> :
        <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Data</TableCell>
                <TableCell>Litros</TableCell>
                <TableCell>Preço/L (R$)</TableCell>
                <TableCell>Total (R$)</TableCell>
                <TableCell>Odômetro (km)</TableCell>
                <TableCell>Fornecedor</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {records.length === 0 && (
                <TableRow><TableCell colSpan={6} align="center">Nenhum abastecimento registrado.</TableCell></TableRow>
              )}
              {records.map(r => (
                <TableRow key={r.id}>
                  <TableCell>{r.date}</TableCell>
                  <TableCell>{r.litersSupplied.toFixed(3)}</TableCell>
                  <TableCell>{r.pricePerLiter.toFixed(4)}</TableCell>
                  <TableCell>{r.totalBrl.toFixed(2)}</TableCell>
                  <TableCell>{r.odometer ?? '-'}</TableCell>
                  <TableCell>{r.supplier ?? '-'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={createOpen} onClose={() => !saving && setCreateOpen(false)} maxWidth="xs" fullWidth>
        <form onSubmit={handleRecord}>
          <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
            Registrar Abastecimento
            <IconButton onClick={() => !saving && setCreateOpen(false)} size="small"><X size={18} /></IconButton>
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Data" value={formDate} onChange={e => setFormDate(e.target.value)} required fullWidth size="small" type="date" slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label="Litros abastecidos" value={formLiters} onChange={e => setFormLiters(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.001' } }} />
              <TextField label="Preço por litro (R$)" value={formPrice} onChange={e => setFormPrice(e.target.value)} required fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0, step: '0.0001' } }} />
              <TextField label="Odômetro (km)" value={formOdometer} onChange={e => setFormOdometer(e.target.value)} fullWidth size="small" type="number" slotProps={{ htmlInput: { min: 0 } }} />
              <TextField label="Fornecedor" value={formSupplier} onChange={e => setFormSupplier(e.target.value)} fullWidth size="small" />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => !saving && setCreateOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" variant="contained" disabled={!formDate || !formLiters || !formPrice || saving}>
              {saving ? 'Registrando...' : 'Registrar'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
