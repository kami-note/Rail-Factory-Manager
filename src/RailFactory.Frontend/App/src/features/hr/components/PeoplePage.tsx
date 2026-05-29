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
import { Users, Plus, PowerOff, Zap } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { Authorized } from '../../auth';
import { activatePerson, deactivatePerson } from '../api/hr';
import { usePeople } from '../hooks/usePeople';
import { CreatePersonModal } from './CreatePersonModal';
import type { Person } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type PeoplePageProps = { tenantCode: string };

type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; name: string };

export function PeoplePage({ tenantCode }: PeoplePageProps) {
  const { data, loading, error: fetchError } = usePeople(tenantCode);
  const [people, setPeople] = useState<Person[]>([]);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (data) setPeople(data); }, [data]);

  const handleCreated = (p: Person) => {
    setPeople(prev => [p, ...prev]);
    setCreateOpen(false);
    setMutationError(null);
    setSuccess(`Pessoa "${p.name}" cadastrada com sucesso.`);
  };

  const handleConfirm = async () => {
    if (!confirm) return;
    setConfirming(true);
    setMutationError(null);
    setSuccess(null);
    try {
      if (confirm.type === 'deactivate') {
        await deactivatePerson(tenantCode, confirm.id);
        setPeople(prev => prev.map(p => p.id === confirm.id
          ? { ...p, status: { key: 'inactive', label: 'Inativo', color: 'default' } }
          : p));
        setSuccess('Pessoa inativada.');
      } else {
        await activatePerson(tenantCode, confirm.id);
        setPeople(prev => prev.map(p => p.id === confirm.id
          ? { ...p, status: { key: 'active', label: 'Ativo', color: 'success' } }
          : p));
        setSuccess('Pessoa ativada.');
      }
      setConfirm(null);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Não foi possível realizar a operação.'));
    } finally {
      setConfirming(false);
    }
  };

  if (loading) return <Box sx={{ p: 6, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (fetchError && people.length === 0) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="PESSOAS"
        icon={<Users size={20} />}
        action={
          <Authorized permission="hr.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
              Nova Pessoa
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
              <TableCell sx={{ fontWeight: 800 }}>NOME</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>CPF/DOC</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>TIPO</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
              <TableCell sx={{ fontWeight: 800 }}>E-MAIL</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {people.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  Nenhuma pessoa cadastrada.
                </TableCell>
              </TableRow>
            ) : people.map(p => (
              <TableRow key={p.id} hover>
                <TableCell sx={{ fontWeight: 600 }}>{p.name}</TableCell>
                <TableCell sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>{p.documentNumber}</TableCell>
                <TableCell><StatusChip status={p.type} /></TableCell>
                <TableCell><StatusChip status={p.status} /></TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>{p.email ?? '—'}</TableCell>
                <TableCell align="right">
                  <Authorized permission="hr.write">
                    {p.status.key === 'active' && (
                      <Tooltip title="Inativar">
                        <IconButton size="small" color="warning" onClick={() => setConfirm({ type: 'deactivate', id: p.id, name: p.name })}>
                          <PowerOff size={16} />
                        </IconButton>
                      </Tooltip>
                    )}
                    {p.status.key === 'inactive' && (
                      <Tooltip title="Ativar">
                        <IconButton size="small" color="success" onClick={() => setConfirm({ type: 'activate', id: p.id, name: p.name })}>
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
      <CreatePersonModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      {/* Modal de confirmação */}
      <ConfirmDialog
        open={confirm !== null}
        title={confirm?.type === 'deactivate' ? 'Inativar pessoa?' : 'Ativar pessoa?'}
        message={
          confirm?.type === 'deactivate'
            ? `"${confirm?.name}" será marcada como inativa.`
            : `"${confirm?.name}" voltará ao status ativo.`
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
