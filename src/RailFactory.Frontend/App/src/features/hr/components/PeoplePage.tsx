import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  alpha,
  useTheme,
} from '@mui/material';
import { Users, Plus, PowerOff, Zap, Download } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import { PageError } from '../../../shared/components/common/PageError';
import { ConfirmDialog } from '../../../shared/components/common/ConfirmDialog';
import { Authorized } from '../../auth';
import { activatePerson, deactivatePerson } from '../api/hr';
import { usePeople } from '../hooks/usePeople';
import { CreatePersonModal } from './CreatePersonModal';
import { PersonDetailPanel } from './PersonDetailPanel';
import type { Person } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type PeoplePageProps = { tenantCode: string };
type ConfirmAction = { type: 'deactivate' | 'activate'; id: string; name: string };

export function PeoplePage({ tenantCode }: PeoplePageProps) {
  const theme = useTheme();
  const { data, loading, error: fetchError } = usePeople(tenantCode);
  const [people, setPeople] = useState<Person[]>([]);
  const [selectedPersonId, setSelectedPersonId] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [confirm, setConfirm] = useState<ConfirmAction | null>(null);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => { if (data) setPeople(data); }, [data]);

  const selectedPerson = useMemo(() => people.find(p => p.id === selectedPersonId), [people, selectedPersonId]);

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

  const now = new Date();

  return (
    <Box sx={{ height: 'calc(100vh - 140px)', display: 'flex', flexDirection: 'column', p: 3 }}>
      <ModuleHeader
        label="PESSOAS"
        icon={<Users size={20} />}
        action={
          <Stack direction="row" spacing={1}>
            <Authorized permission="hr.read">
              <Tooltip title="Exportar lista de pessoas (CSV)">
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<Download size={14} />}
                  onClick={() => window.open('/api/hr/people/export', '_blank')}
                >
                  Exportar
                </Button>
              </Tooltip>
              <Tooltip title="Exportar folha de pagamento do mês atual (CSV)">
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<Download size={14} />}
                  onClick={() => window.open(`/api/hr/payroll/export?year=${now.getFullYear()}&month=${now.getMonth() + 1}`, '_blank')}
                >
                  Folha
                </Button>
              </Tooltip>
            </Authorized>
            <Authorized permission="hr.write">
              <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
                Nova Pessoa
              </Button>
            </Authorized>
          </Stack>
        }
      />

      {success && <Alert severity="success" onClose={() => setSuccess(null)} sx={{ mb: 2 }}>{success}</Alert>}
      {mutationError && <Alert severity="error" onClose={() => setMutationError(null)} sx={{ mb: 2 }}>{mutationError}</Alert>}

      <Box sx={{ flexGrow: 1, overflow: 'hidden', display: 'flex', gap: 2, mt: 2 }}>
        <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
          <TableContainer component={Paper} variant="outlined">
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
                  <TableRow
                    key={p.id}
                    hover
                    selected={selectedPersonId === p.id}
                    onClick={() => setSelectedPersonId(p.id)}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell sx={{ fontWeight: 600 }}>
                      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                        <Box
                          sx={{
                            width: 32,
                            height: 32,
                            borderRadius: '50%',
                            bgcolor: alpha(theme.palette.primary.main, 0.08),
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            overflow: 'hidden',
                            border: '1px solid',
                            borderColor: 'divider',
                            flexShrink: 0
                          }}
                        >
                          {p.imageUrl ? (
                            <Box
                              component="img"
                              src={p.imageUrl}
                              sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
                            />
                          ) : (
                            <Users size={16} color={theme.palette.primary.main} />
                          )}
                        </Box>
                        <span>{p.name}</span>
                      </Stack>
                    </TableCell>
                    <TableCell sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>{p.documentNumber}</TableCell>
                    <TableCell><StatusChip status={p.type} /></TableCell>
                    <TableCell><StatusChip status={p.status} /></TableCell>
                    <TableCell sx={{ color: 'text.secondary' }}>{p.email ?? '—'}</TableCell>
                    <TableCell align="right" onClick={e => e.stopPropagation()}>
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
        </Box>

        {selectedPerson && (
          <Paper
            variant="outlined"
            sx={{
              width: 420,
              flexShrink: 0,
              overflowY: 'auto',
              bgcolor: 'background.paper',
              borderLeft: 2,
              borderColor: alpha(theme.palette.primary.main, 0.2),
            }}
          >
            <PersonDetailPanel
              key={selectedPerson.id}
              tenantCode={tenantCode}
              person={selectedPerson}
              onUpdated={(updated) => {
                setPeople(prev => prev.map(p => p.id === updated.id ? updated : p));
              }}
            />
          </Paper>
        )}
      </Box>

      <CreatePersonModal
        open={createOpen}
        tenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

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
