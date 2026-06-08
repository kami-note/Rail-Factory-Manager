import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, IconButton,
  Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Tooltip,
} from '@mui/material';
import { Truck, Plus, PowerOff, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { activateCarrier, deactivateCarrier } from '../api/logistics';
import { useCarriers } from '../hooks/useCarriers';
import { CreateCarrierModal } from './CreateCarrierModal';
import type { Carrier } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { Masks } from '../../../shared/lib/utils/masks';

type Props = { tenantCode: string };
type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; name: string };

export function CarriersPage({ tenantCode }: Props) {
  const { data, loading, error: fetchError } = useCarriers(tenantCode);
  const [carriers, setCarriers] = useState<Carrier[]>([]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (data) setCarriers(data); }, [data]);

  const handleCreated = (c: Carrier) => {
    setCarriers(prev => [c, ...prev]);
    setCreateOpen(false);
    setMutationError(null);
    setSuccess(`Transportadora "${c.name}" cadastrada com sucesso.`);
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true);
    setMutationError(null); setSuccess(null);
    try {
      if (confirm.type === 'deactivate') {
        await deactivateCarrier(tenantCode, confirm.id);
        setCarriers(prev => prev.map(c => c.id === confirm.id ? { ...c, status: 'Inactive' as const } : c));
        setSuccess(`Transportadora "${confirm.name}" inativada.`);
      } else {
        await activateCarrier(tenantCode, confirm.id);
        setCarriers(prev => prev.map(c => c.id === confirm.id ? { ...c, status: 'Active' as const } : c));
        setSuccess(`Transportadora "${confirm.name}" ativada.`);
      }
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao alterar status da transportadora.'));
    } finally {
      setConfirming(false);
      setConfirm(null);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Transportadoras" icon={<Truck size={20} />} />

      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
        <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
          Nova Transportadora
        </Button>
      </Box>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Nome</TableCell>
              <TableCell>CNPJ / Documento</TableCell>
              <TableCell>Taxa/kg (R$)</TableCell>
              <TableCell>Taxa/m³ (R$)</TableCell>
              <TableCell>Status</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {carriers.length === 0 && (
              <TableRow><TableCell colSpan={6} align="center">Nenhuma transportadora cadastrada.</TableCell></TableRow>
            )}
            {carriers.map(c => (
              <TableRow key={c.id}>
                <TableCell>{c.name}</TableCell>
                <TableCell sx={{ fontFamily: 'monospace' }}>{Masks.cpfCnpj(c.documentNumber)}</TableCell>
                <TableCell>{c.ratePerKg.toFixed(4)}</TableCell>
                <TableCell>{c.ratePerCbm.toFixed(4)}</TableCell>
                <TableCell>
                  <StatusChip status={{ key: c.status.toLowerCase(), label: c.status === 'Active' ? 'Ativo' : 'Inativo', color: c.status === 'Active' ? 'success' : 'default' }} />
                </TableCell>
                <TableCell align="right">
                  {c.status === 'Active' ? (
                    <Tooltip title="Inativar">
                      <IconButton size="small" onClick={() => setConfirm({ type: 'deactivate', id: c.id, name: c.name })}><PowerOff size={15} /></IconButton>
                    </Tooltip>
                  ) : (
                    <Tooltip title="Ativar">
                      <IconButton size="small" onClick={() => setConfirm({ type: 'activate', id: c.id, name: c.name })}><Zap size={15} /></IconButton>
                    </Tooltip>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <CreateCarrierModal open={createOpen} tenantCode={tenantCode} onCreated={handleCreated} onClose={() => setCreateOpen(false)} />

      <ConfirmDialog
        open={!!confirm}
        title={confirm?.type === 'deactivate' ? 'Inativar Transportadora' : 'Ativar Transportadora'}
        message={confirm?.type === 'deactivate'
          ? `Tem certeza que deseja inativar "${confirm?.name}"?`
          : `Tem certeza que deseja ativar "${confirm?.name}"?`}
        confirmLabel={confirm?.type === 'deactivate' ? 'Inativar' : 'Ativar'}
        severity={confirm?.type === 'deactivate' ? 'error' : 'primary'}
        loading={confirming}
        onConfirm={handleConfirm}
        onCancel={() => setConfirm(null)}
      />
    </Box>
  );
}
