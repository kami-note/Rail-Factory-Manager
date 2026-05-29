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
import { Factory, Plus, PowerOff, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { Authorized } from '../../auth';
import { activateWorkCenter, deactivateWorkCenter } from '../api/production';
import { useWorkCenters } from '../hooks/useWorkCenters';
import { CreateWorkCenterModal } from './CreateWorkCenterModal';
import type { WorkCenter } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type WorkCentersPageProps = { tenantCode: string };

type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; name: string };

/**
 * Management page for production work centers.
 * Lists active/inactive centers and allows creating or deactivating them.
 */
export function WorkCentersPage({ tenantCode }: WorkCentersPageProps) {
  const { data, loading, error: fetchError } = useWorkCenters(tenantCode);
  const [centers, setCenters] = useState<WorkCenter[]>([]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (data) setCenters(data); }, [data]);

  const handleCreated = (wc: WorkCenter) => {
    setCenters(prev => [wc, ...prev]);
    setCreateOpen(false);
    setMutationError(null);
    setSuccess(`Centro "${wc.name}" criado com sucesso.`);
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true);
    setMutationError(null);
    setSuccess(null);
    try {
      if (confirm.type === 'deactivate') {
        await deactivateWorkCenter(tenantCode, confirm.id);
        setCenters(prev => prev.map(c => c.id === confirm.id
          ? { ...c, status: { key: 'Inactive', label: 'Inativo', color: 'default' } }
          : c));
        setSuccess('Centro de trabalho desativado.');
      } else {
        await activateWorkCenter(tenantCode, confirm.id);
        setCenters(prev => prev.map(c => c.id === confirm.id
          ? { ...c, status: { key: 'Active', label: 'Ativo', color: 'success' } }
          : c));
        setSuccess('Centro de trabalho ativado.');
      }
      setConfirm(null);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Não foi possível realizar a operação.'));
    } finally {
      setConfirming(false);
    }
  };

  if (loading) return <Box sx={{ p: 6, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (fetchError && centers.length === 0) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="CENTROS DE TRABALHO"
        icon={<Factory size={20} />}
        action={
          <Authorized permission="production.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
              Novo Centro
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
              <TableCell sx={{ fontWeight: 800 }}>CÓDIGO</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>NOME</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>CRIADO EM</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {centers.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  Nenhum centro de trabalho cadastrado.
                </TableCell>
              </TableRow>
            ) : centers.map(wc => (
              <TableRow key={wc.id} hover>
                <TableCell sx={{ fontWeight: 700, fontFamily: 'monospace' }}>{wc.code}</TableCell>
                <TableCell>{wc.name}</TableCell>
                <TableCell><StatusChip status={wc.status} /></TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>
                  {new Date(wc.createdAt).toLocaleDateString('pt-BR')}
                </TableCell>
                <TableCell align="right">
                  <Authorized permission="production.write">
                    {wc.status.key === 'Active' && (
                      <Tooltip title="Desativar">
                        <IconButton size="small" color="warning" onClick={() => setConfirm({ type: 'deactivate', id: wc.id, name: wc.name })}>
                          <PowerOff size={16} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {wc.status.key === 'Inactive' && (
                      <Tooltip title="Ativar">
                        <IconButton size="small" color="success" onClick={() => setConfirm({ type: 'activate', id: wc.id, name: wc.name })}>
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
      <CreateWorkCenterModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      {/* Modal de confirmação */}
      <ConfirmDialog
        open={confirm !== null}
        title={confirm?.type === 'deactivate' ? 'Desativar centro de trabalho?' : 'Ativar centro de trabalho?'}
        message={
          confirm?.type === 'deactivate'
            ? `O centro "${confirm?.name}" não poderá receber novas ordens enquanto estiver inativo.`
            : `O centro "${confirm?.name}" voltará a aceitar novas ordens de produção.`
        }
        severity={confirm?.type === 'deactivate' ? 'warning' : 'primary'}
        confirmLabel={confirm?.type === 'deactivate' ? 'Desativar' : 'Ativar'}
        loading={confirming}
        onConfirm={() => void handleConfirm()}
        onCancel={() => setConfirm(null)}
      />
    </Box>
  );
}
