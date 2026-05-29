import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
} from '@mui/material';
import { Truck, Plus, PowerOff, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { Authorized } from '../../auth';
import { activateVehicle, deactivateVehicle } from '../api/fleet';
import { useVehicles } from '../hooks/useVehicles';
import { CreateVehicleModal } from './CreateVehicleModal';
import type { Vehicle } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type VehiclesPageProps = { tenantCode: string };

type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; plate: string };

export function VehiclesPage({ tenantCode }: VehiclesPageProps) {
  const { data, loading, error: fetchError } = useVehicles(tenantCode);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (data) setVehicles(data); }, [data]);

  const handleCreated = (v: Vehicle) => {
    setVehicles(prev => [v, ...prev]);
    setCreateOpen(false);
    setMutationError(null);
    setSuccess(`Veículo "${v.plate}" cadastrado com sucesso.`);
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true);
    setMutationError(null);
    setSuccess(null);
    try {
      if (confirm.type === 'deactivate') {
        await deactivateVehicle(tenantCode, confirm.id);
        setVehicles(prev => prev.map(v => v.id === confirm.id
          ? { ...v, status: { key: 'inactive', label: 'Inativo', color: 'default' } }
          : v));
        setSuccess('Veículo inativado.');
      } else {
        await activateVehicle(tenantCode, confirm.id);
        setVehicles(prev => prev.map(v => v.id === confirm.id
          ? { ...v, status: { key: 'active', label: 'Ativo', color: 'success' } }
          : v));
        setSuccess('Veículo ativado.');
      }
      setConfirm(null);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Não foi possível realizar a operação.'));
    } finally {
      setConfirming(false);
    }
  };

  if (loading) return <Box sx={{ p: 6, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (fetchError && vehicles.length === 0) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="FROTA DE VEÍCULOS"
        icon={<Truck size={20} />}
        action={
          <Authorized permission="fleet.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
              Novo Veículo
            </Button>
          </Authorized>
        }
      />

      {success && <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>{success}</Alert>}
      {mutationError && <Alert severity="error" onClose={() => setMutationError(null)} sx={{ mb: 2 }}>{mutationError}</Alert>}

      <TableContainer component={Paper} variant="outlined" sx={{ mt: 3 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 800 }}>PLACA</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>TIPO</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>CARGA MÁX (kg)</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>VOLUME MÁX (m³)</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>VENC. CRLV</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {vehicles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  Nenhum veículo cadastrado.
                </TableCell>
              </TableRow>
            ) : vehicles.map(v => (
              <TableRow key={v.id} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{v.plate}</TableCell>
                <TableCell><StatusChip status={v.type} /></TableCell>
                <TableCell><StatusChip status={v.status} /></TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>{v.maxWeightKg.toLocaleString('pt-BR')}</TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>{v.maxVolumeCbm.toLocaleString('pt-BR', { maximumFractionDigits: 3 })}</TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>{new Date(v.licenseExpiry).toLocaleDateString('pt-BR')}</TableCell>
                <TableCell align="right">
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

      {/* Modal de criação */}
      <CreateVehicleModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      {/* Modal de confirmação */}
      <ConfirmDialog
        open={confirm !== null}
        title={confirm?.type === 'deactivate' ? 'Inativar veículo?' : 'Ativar veículo?'}
        message={
          confirm?.type === 'deactivate'
            ? `O veículo "${confirm?.plate}" não poderá receber novas alocações de motorista enquanto estiver inativo.`
            : `O veículo "${confirm?.plate}" voltará ao status ativo.`
        }
        severity={confirm?.type === 'deactivate' ? 'warning' : 'primary'}
        confirmLabel={confirm?.type === 'deactivate' ? 'Inativar' : 'Ativar'}
        loading={confirming}
        onConfirm={() => void handleConfirm()}
        onCancel={() => setConfirm(null)}
      />
    </Box>
  );
}
