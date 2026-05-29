import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, FormControl, InputLabel, MenuItem,
  Paper, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Dialog, DialogTitle, DialogContent, DialogActions, IconButton, Tooltip,
} from '@mui/material';
import { Wrench, Plus, CheckCircle, XCircle, X } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { useVehicles } from '../hooks/useVehicles';
import {
  listMaintenancePlans, scheduleMaintenance, completeMaintenance, cancelMaintenance,
  type MaintenancePlan, type MaintenanceType,
} from '../api/fleet-maintenance';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };

const TYPE_OPTIONS: { value: MaintenanceType; label: string }[] = [
  { value: 'Preventive', label: 'Preventiva' },
  { value: 'Corrective', label: 'Corretiva' },
];

const STATUS_COLOR: Record<string, 'default' | 'info' | 'success' | 'error'> = {
  Scheduled: 'info', Done: 'success', Cancelled: 'error',
};
const STATUS_LABEL: Record<string, string> = {
  Scheduled: 'Agendada', Done: 'Concluída', Cancelled: 'Cancelada',
};

export function MaintenancePage({ tenantCode }: Props) {
  const { data: vehicles, loading: vehiclesLoading, error: vehiclesError } = useVehicles(tenantCode);
  const [vehicleId, setVehicleId] = useState('');
  const [plans, setPlans] = useState<MaintenancePlan[]>([]);
  const [loadingPlans, setLoadingPlans] = useState(false);
  const [plansError, setPlansError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  // form state
  const [formType, setFormType] = useState<MaintenanceType>('Preventive');
  const [formDescription, setFormDescription] = useState('');
  const [formDate, setFormDate] = useState('');
  const [formNotes, setFormNotes] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!vehicleId) return;
    setLoadingPlans(true); setPlansError(null);
    listMaintenancePlans(tenantCode, vehicleId)
      .then(setPlans)
      .catch(err => setPlansError(toUiErrorMessage(err, 'Erro ao carregar manutenções.')))
      .finally(() => setLoadingPlans(false));
  }, [tenantCode, vehicleId]);

  const handleSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vehicleId || saving) return;
    setSaving(true); setMutationError(null);
    try {
      const plan = await scheduleMaintenance(tenantCode, vehicleId, {
        type: formType, description: formDescription.trim(),
        scheduledDate: formDate, notes: formNotes.trim() || undefined,
      });
      setPlans(prev => [plan, ...prev]);
      setCreateOpen(false);
      setFormDescription(''); setFormDate(''); setFormNotes('');
      setSuccess('Manutenção agendada com sucesso.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao agendar manutenção.'));
    } finally { setSaving(false); }
  };

  const handleComplete = async (planId: string) => {
    if (!vehicleId) return;
    const today = new Date().toISOString().slice(0, 10);
    try {
      await completeMaintenance(tenantCode, vehicleId, planId, today);
      setPlans(prev => prev.map(p => p.id === planId ? { ...p, status: 'Done', completedDate: today } : p));
      setSuccess('Manutenção concluída.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao concluir manutenção.'));
    }
  };

  const handleCancel = async (planId: string) => {
    if (!vehicleId) return;
    try {
      await cancelMaintenance(tenantCode, vehicleId, planId);
      setPlans(prev => prev.map(p => p.id === planId ? { ...p, status: 'Cancelled' } : p));
      setSuccess('Manutenção cancelada.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao cancelar manutenção.'));
    }
  };

  if (vehiclesLoading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (vehiclesError) return <PageError message={vehiclesError} />;

  const activeVehicles = vehicles?.filter(v => v.status?.key === 'active' || (v as any).status === 'Active') ?? [];

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Manutenção de Veículos" icon={<Wrench size={20} />} />

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
            Agendar Manutenção
          </Button>
        )}
      </Stack>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      {vehicleId && (
        loadingPlans ? <CircularProgress /> :
        plansError ? <Alert severity="error">{plansError}</Alert> :
        <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Tipo</TableCell>
                <TableCell>Descrição</TableCell>
                <TableCell>Data Agendada</TableCell>
                <TableCell>Data Concluída</TableCell>
                <TableCell>Status</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {plans.length === 0 && (
                <TableRow><TableCell colSpan={6} align="center">Nenhuma manutenção registrada.</TableCell></TableRow>
              )}
              {plans.map(p => (
                <TableRow key={p.id}>
                  <TableCell>{p.type === 'Preventive' ? 'Preventiva' : 'Corretiva'}</TableCell>
                  <TableCell>{p.description}</TableCell>
                  <TableCell>{p.scheduledDate}</TableCell>
                  <TableCell>{p.completedDate ?? '-'}</TableCell>
                  <TableCell><Chip label={STATUS_LABEL[p.status]} color={STATUS_COLOR[p.status]} size="small" /></TableCell>
                  <TableCell align="right">
                    {p.status === 'Scheduled' && (
                      <>
                        <Tooltip title="Concluir">
                          <IconButton size="small" color="success" onClick={() => handleComplete(p.id)}><CheckCircle size={15} /></IconButton>
                        </Tooltip>
                        <Tooltip title="Cancelar">
                          <IconButton size="small" color="error" onClick={() => handleCancel(p.id)}><XCircle size={15} /></IconButton>
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
              <FormControl fullWidth size="small">
                <InputLabel>Tipo</InputLabel>
                <Select value={formType} label="Tipo" onChange={e => setFormType(e.target.value as MaintenanceType)}>
                  {TYPE_OPTIONS.map(o => <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>)}
                </Select>
              </FormControl>
              <TextField label="Descrição" value={formDescription} onChange={e => setFormDescription(e.target.value)} required fullWidth size="small" />
              <TextField label="Data Agendada" value={formDate} onChange={e => setFormDate(e.target.value)} required fullWidth size="small" type="date" slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label="Observações" value={formNotes} onChange={e => setFormNotes(e.target.value)} fullWidth size="small" multiline rows={2} />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => !saving && setCreateOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" variant="contained" disabled={!formDescription.trim() || !formDate || saving}>
              {saving ? 'Agendando...' : 'Agendar'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  );
}
