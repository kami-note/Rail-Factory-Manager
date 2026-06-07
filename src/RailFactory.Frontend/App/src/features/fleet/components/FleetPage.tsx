import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress,
  IconButton, InputAdornment, Paper, Stack, Tab, Tabs, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from '@mui/material';
import { BarChart3, Fuel, Plus, PowerOff, Search, Truck, UserCheck, Wrench, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { Authorized } from '../../auth';
import { activateVehicle, deactivateVehicle } from '../api/fleet';
import { useVehicles } from '../hooks/useVehicles';
import { CreateVehicleModal } from './CreateVehicleModal';
import { VehicleDetailPanel } from './VehicleDetailPanel';
import { MaintenanceContent } from './MaintenanceContent';
import { FuelingContent } from './FuelingContent';
import { ReportsContent } from './ReportsContent';
import type { Vehicle } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };
type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; plate: string };

export function FleetPage({ tenantCode }: Props) {
  const { data, loading, error: fetchError } = useVehicles(tenantCode);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [tab, setTab] = useState(0);

  useEffect(() => { if (data) setVehicles(data); }, [data]);

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');

  const filteredVehicles = useMemo(() => vehicles.filter(v => {
    if (search && !v.plate.toLowerCase().includes(search.toLowerCase())) return false;
    if (statusFilter !== 'all' && v.status.key !== statusFilter) return false;
    return true;
  }), [vehicles, search, statusFilter]);

  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [detailVehicle, setDetailVehicle] = useState<Vehicle | null>(null);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  const handleCreated = (v: Vehicle) => {
    setVehicles(prev => [v, ...prev]);
    setCreateOpen(false);
    setSuccess(`Veículo "${v.plate}" cadastrado.`);
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true); setMutationError(null);
    try {
      if (confirm.type === 'deactivate') {
        await deactivateVehicle(tenantCode, confirm.id);
        setVehicles(prev => prev.map(v => v.id === confirm.id
          ? { ...v, status: { key: 'inactive', label: 'Inativo', color: 'default' } } : v));
        setSuccess('Veículo inativado.');
      } else {
        await activateVehicle(tenantCode, confirm.id);
        setVehicles(prev => prev.map(v => v.id === confirm.id
          ? { ...v, status: { key: 'active', label: 'Ativo', color: 'success' } } : v));
        setSuccess('Veículo ativado.');
      }
      setConfirm(null);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao atualizar veículo.'));
    } finally { setConfirming(false); }
  };

  if (loading) return <Box sx={{ p: 6, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (fetchError && vehicles.length === 0) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="Frota"
        icon={<Truck size={20} />}
        action={
          tab === 0 ? (
            <Authorized permission="fleet.write">
              <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
                Novo Veículo
              </Button>
            </Authorized>
          ) : undefined
        }
      />

      <Tabs
        value={tab}
        onChange={(_, v) => setTab(v)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Veículos" icon={<Truck size={15} />} iconPosition="start" />
        <Tab label="Manutenção" icon={<Wrench size={15} />} iconPosition="start" />
        <Tab label="Abastecimento" icon={<Fuel size={15} />} iconPosition="start" />
        <Tab label="Relatórios" icon={<BarChart3 size={15} />} iconPosition="start" />
      </Tabs>

      {/* Tab 0 — Veículos */}
      {tab === 0 && (
        <>
          <Stack direction="row" spacing={2} sx={{ mb: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <TextField
              placeholder="Buscar placa…"
              value={search}
              onChange={e => setSearch(e.target.value)}
              size="small"
              sx={{ width: 200 }}
              slotProps={{ input: { startAdornment: <InputAdornment position="start"><Search size={15} /></InputAdornment> } }}
            />
            <Stack direction="row" spacing={1}>
              {(['all', 'active', 'inactive'] as const).map(s => (
                <Chip
                  key={s}
                  label={s === 'all' ? 'Todos' : s === 'active' ? 'Ativos' : 'Inativos'}
                  size="small"
                  color={statusFilter === s ? (s === 'active' ? 'success' : s === 'inactive' ? 'default' : 'primary') : 'default'}
                  variant={statusFilter === s ? 'filled' : 'outlined'}
                  onClick={() => setStatusFilter(s)}
                  sx={{ cursor: 'pointer' }}
                />
              ))}
            </Stack>
            {filteredVehicles.length !== vehicles.length && (
              <Typography variant="caption" color="text.secondary">
                {filteredVehicles.length} de {vehicles.length}
              </Typography>
            )}
          </Stack>
        <TableContainer component={Paper} variant="outlined">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 800 }}>PLACA</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>TIPO</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>RNTRC</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>CARGA MÁX (kg)</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>VOLUME MÁX (m³)</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>VENC. CRLV</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredVehicles.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                    {vehicles.length === 0 ? 'Nenhum veículo cadastrado.' : 'Nenhum veículo encontrado.'}
                  </TableCell>
                </TableRow>
              ) : filteredVehicles.map(v => (
                <TableRow
                  key={v.id}
                  hover
                  onClick={() => setDetailVehicle(v)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{v.plate}</TableCell>
                  <TableCell><StatusChip status={v.type} /></TableCell>
                  <TableCell><StatusChip status={v.status} /></TableCell>
                  <TableCell sx={{ fontFamily: 'monospace', color: v.rntrc ? 'text.primary' : 'text.disabled', fontSize: 13 }}>
                    {v.rntrc ?? '—'}
                  </TableCell>
                  <TableCell sx={{ color: 'text.secondary' }}>{v.maxWeightKg.toLocaleString('pt-BR')}</TableCell>
                  <TableCell sx={{ color: 'text.secondary' }}>{v.maxVolumeCbm.toLocaleString('pt-BR', { maximumFractionDigits: 3 })}</TableCell>
                  <TableCell sx={{ color: 'text.secondary' }}>{new Date(v.licenseExpiry).toLocaleDateString('pt-BR')}</TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }} onClick={e => e.stopPropagation()}>
                    <Authorized permission="fleet.write">
                      {v.status.key === 'active' && (
                        <Tooltip title="Inativar">
                          <IconButton size="small" color="warning" onClick={() => setConfirm({ type: 'deactivate', id: v.id, plate: v.plate })}>
                            <PowerOff size={16} />
                          </IconButton>
                        </Tooltip>
                      )}
                      {v.status.key === 'inactive' && (
                        <Tooltip title="Ativar">
                          <IconButton size="small" color="success" onClick={() => setConfirm({ type: 'activate', id: v.id, plate: v.plate })}>
                            <Zap size={16} />
                          </IconButton>
                        </Tooltip>
                      )}
                    </Authorized>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
        </>
      )}

      {/* Tab 1 — Manutenção (nível frota) */}
      {tab === 1 && <MaintenanceContent tenantCode={tenantCode} vehicles={vehicles} />}

      {/* Tab 2 — Abastecimento (nível frota) */}
      {tab === 2 && <FuelingContent tenantCode={tenantCode} vehicles={vehicles} />}

      {/* Tab 3 — Relatórios */}
      {tab === 3 && <ReportsContent tenantCode={tenantCode} vehicles={vehicles} />}

      {/* Painel de detalhe do veículo */}
      {detailVehicle && (
        <VehicleDetailPanel
          vehicle={vehicles.find(v => v.id === detailVehicle.id) ?? detailVehicle}
          tenantCode={tenantCode}
          onClose={() => setDetailVehicle(null)}
        />
      )}

      {createOpen && (
        <CreateVehicleModal open tenantCode={tenantCode} onCreated={handleCreated} onClose={() => setCreateOpen(false)} />
      )}

      <ConfirmDialog
        open={confirm !== null}
        title={confirm?.type === 'deactivate' ? 'Inativar veículo?' : 'Ativar veículo?'}
        message={
          confirm?.type === 'deactivate'
            ? `O veículo "${confirm?.plate}" não poderá receber novas alocações enquanto estiver inativo.`
            : `O veículo "${confirm?.plate}" voltará ao status ativo.`
        }
        severity={confirm?.type === 'deactivate' ? 'warning' : 'primary'}
        confirmLabel={confirm?.type === 'deactivate' ? 'Inativar' : 'Ativar'}
        loading={confirming}
        onConfirm={() => void handleConfirm()}
        onCancel={() => setConfirm(null)}
      />

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
