import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, FormControl, InputLabel, MenuItem,
  Paper, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, Tooltip, Typography,
} from '@mui/material';
import { Plus, CheckCircle, XCircle, X } from 'lucide-react';
import {
  listMaintenancePlans, scheduleMaintenance, completeMaintenance, cancelMaintenance,
  type MaintenancePlan, type MaintenanceType,
} from '../api/fleet-maintenance';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { Vehicle } from '../types';

type Props = {
  tenantCode: string;
  vehicleId?: string;
  vehicles?: Vehicle[];
};

const TYPE_OPTIONS: { value: MaintenanceType; label: string }[] = [
  { value: 'Preventive', label: 'Preventiva' },
  { value: 'Corrective', label: 'Corretiva' },
];

const STATUS: Record<string, { label: string; color: 'default' | 'info' | 'success' | 'error' }> = {
  Scheduled: { label: 'Agendada', color: 'info' },
  Done: { label: 'Concluída', color: 'success' },
  Cancelled: { label: 'Cancelada', color: 'error' },
};

export function MaintenanceContent({ tenantCode, vehicleId: vehicleIdProp, vehicles }: Props) {
  const fleetMode = !vehicleIdProp;

  const [plans, setPlans] = useState<MaintenancePlan[]>([]);
  const [filterVehicleId, setFilterVehicleId] = useState('');
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [formVehicleId, setFormVehicleId] = useState('');
  const [formType, setFormType] = useState<MaintenanceType>('Preventive');
  const [formDescription, setFormDescription] = useState('');
  const [formDate, setFormDate] = useState('');
  const [formNotes, setFormNotes] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (fleetMode) {
      if (!vehicles || vehicles.length === 0) return;
      setLoading(true); setLoadError(null);
      Promise.all(vehicles.map(v => listMaintenancePlans(tenantCode, v.id)))
        .then(results => {
          const combined = results.flat().sort((a, b) => b.scheduledDate.localeCompare(a.scheduledDate));
          setPlans(combined);
        })
        .catch(err => setLoadError(toUiErrorMessage(err, 'Erro ao carregar manutenções.')))
        .finally(() => setLoading(false));
    } else {
      setLoading(true); setLoadError(null);
      listMaintenancePlans(tenantCode, vehicleIdProp!)
        .then(setPlans)
        .catch(err => setLoadError(toUiErrorMessage(err, 'Erro ao carregar manutenções.')))
        .finally(() => setLoading(false));
    }
  }, [tenantCode, vehicleIdProp, vehicles, fleetMode]);

  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');

  const displayPlans = plans.filter(p => {
    if (fleetMode && filterVehicleId && p.vehicleId !== filterVehicleId) return false;
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (typeFilter !== 'all' && p.type !== typeFilter) return false;
    return true;
  });

  const plateLookup = (vehicleId: string) =>
    vehicles?.find(v => v.id === vehicleId)?.plate ?? vehicleId.slice(0, 8);

  const handleSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetId = fleetMode ? formVehicleId : vehicleIdProp!;
    if (!targetId || saving) return;
    setSaving(true); setMutationError(null);
    try {
      const plan = await scheduleMaintenance(tenantCode, targetId, {
        type: formType, description: formDescription.trim(),
        scheduledDate: formDate, notes: formNotes.trim() || undefined,
      });
      setPlans(prev => [plan, ...prev]);
      setCreateOpen(false);
      setFormDescription(''); setFormDate(''); setFormNotes(''); setFormVehicleId('');
      setSuccess('Manutenção agendada.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao agendar manutenção.'));
    } finally { setSaving(false); }
  };

  const handleComplete = async (plan: MaintenancePlan) => {
    const today = new Date().toISOString().slice(0, 10);
    try {
      await completeMaintenance(tenantCode, plan.vehicleId, plan.id, today);
      setPlans(prev => prev.map(p => p.id === plan.id ? { ...p, status: 'Done', completedDate: today } : p));
      setSuccess('Manutenção concluída.');
    } catch (err) { setMutationError(toUiErrorMessage(err, 'Erro ao concluir.')); }
  };

  const handleCancel = async (plan: MaintenancePlan) => {
    try {
      await cancelMaintenance(tenantCode, plan.vehicleId, plan.id);
      setPlans(prev => prev.map(p => p.id === plan.id ? { ...p, status: 'Cancelled' } : p));
      setSuccess('Manutenção cancelada.');
    } catch (err) { setMutationError(toUiErrorMessage(err, 'Erro ao cancelar.')); }
  };

  return (
    <Box>
      {/* Fleet-mode toolbar: filters + schedule button */}
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
              Agendar
            </Button>
          </Stack>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', alignItems: 'center', gap: 0.5 }}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>Status:</Typography>
            {(['all', 'Scheduled', 'Done', 'Cancelled'] as const).map(s => (
              <Chip
                key={s}
                label={s === 'all' ? 'Todos' : STATUS[s]?.label}
                size="small"
                color={statusFilter === s ? (s === 'all' ? 'primary' : STATUS[s]?.color) : 'default'}
                onClick={() => setStatusFilter(s)}
                sx={{ cursor: 'pointer' }}
              />
            ))}
            <Box sx={{ width: 8 }} />
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>Tipo:</Typography>
            {(['all', 'Preventive', 'Corrective'] as const).map(t => (
              <Chip
                key={t}
                label={t === 'all' ? 'Todos' : t === 'Preventive' ? 'Preventiva' : 'Corretiva'}
                size="small"
                color={typeFilter === t ? 'primary' : 'default'}
                onClick={() => setTypeFilter(t)}
                sx={{ cursor: 'pointer' }}
              />
            ))}
          </Stack>
        </Stack>
      )}

      {/* Drawer-mode button */}
      {!fleetMode && (
        <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <Button variant="contained" size="small" startIcon={<Plus size={15} />} onClick={() => setCreateOpen(true)}>
            Agendar Manutenção
          </Button>
        </Box>
      )}

      {loadError && <Alert severity="error" sx={{ mb: 2 }}>{loadError}</Alert>}

      {loading ? <CircularProgress size={24} /> : (
        <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                {fleetMode && <TableCell sx={{ fontWeight: 700 }}>Veículo</TableCell>}
                <TableCell>Tipo</TableCell>
                <TableCell>Descrição</TableCell>
                <TableCell>Agendada</TableCell>
                <TableCell>Concluída</TableCell>
                <TableCell>Status</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {displayPlans.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={fleetMode ? 7 : 6} align="center" sx={{ color: 'text.secondary', py: 3 }}>
                    Nenhuma manutenção registrada.
                  </TableCell>
                </TableRow>
              ) : displayPlans.map(p => (
                <TableRow key={p.id} hover>
                  {fleetMode && (
                    <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace', fontSize: 12 }}>
                      {plateLookup(p.vehicleId)}
                    </TableCell>
                  )}
                  <TableCell>{p.type === 'Preventive' ? 'Preventiva' : 'Corretiva'}</TableCell>
                  <TableCell>{p.description}</TableCell>
                  <TableCell sx={{ fontSize: 12 }}>{p.scheduledDate}</TableCell>
                  <TableCell sx={{ fontSize: 12, color: 'text.secondary' }}>{p.completedDate ?? '—'}</TableCell>
                  <TableCell><Chip label={STATUS[p.status]?.label} color={STATUS[p.status]?.color} size="small" /></TableCell>
                  <TableCell align="right">
                    {p.status === 'Scheduled' && (
                      <>
                        <Tooltip title="Concluir">
                          <IconButton size="small" color="success" onClick={() => handleComplete(p)}><CheckCircle size={15} /></IconButton>
                        </Tooltip>
                        <Tooltip title="Cancelar">
                          <IconButton size="small" color="error" onClick={() => handleCancel(p)}><XCircle size={15} /></IconButton>
                        </Tooltip>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={createOpen} onClose={() => !saving && setCreateOpen(false)} maxWidth="xs" fullWidth>
        <form onSubmit={handleSchedule}>
          <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
            Agendar Manutenção
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
              <FormControl fullWidth size="small">
                <InputLabel>Tipo</InputLabel>
                <Select value={formType} label="Tipo" onChange={e => setFormType(e.target.value as MaintenanceType)}>
                  {TYPE_OPTIONS.map(o => <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>)}
                </Select>
              </FormControl>
              <TextField label="Descrição" value={formDescription} onChange={e => setFormDescription(e.target.value)} required fullWidth size="small" />
              <TextField label="Data Agendada" value={formDate} onChange={e => setFormDate(e.target.value)} required fullWidth size="small" type="date" sx={{ '& label': { transform: 'translate(14px, -9px) scale(0.75)' } }} />
              <TextField label="Observações" value={formNotes} onChange={e => setFormNotes(e.target.value)} fullWidth size="small" multiline rows={2} />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => !saving && setCreateOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" variant="contained"
              disabled={!formDescription.trim() || !formDate || (fleetMode && !formVehicleId) || saving}>
              {saving ? 'Agendando...' : 'Agendar'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
