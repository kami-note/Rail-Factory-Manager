import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Chip,
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
  TextField,
  Tooltip,
  Typography
} from '@mui/material';
import { Factory, Plus, PowerOff } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { InlineError } from '../../../shared/components/common/InlineError';
import { PageError } from '../../../shared/components/common/PageError';
import { Authorized } from '../../auth';
import { listWorkCenters, createWorkCenter, deactivateWorkCenter } from '../api/production';
import type { WorkCenter } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type WorkCentersPageProps = {
  tenantCode: string;
};

export function WorkCentersPage({ tenantCode }: WorkCentersPageProps) {
  const [centers, setCenters] = useState<WorkCenter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setCenters(await listWorkCenters(tenantCode));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível carregar os centros de trabalho.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [tenantCode]);

  const handleCreated = (wc: WorkCenter) => {
    setCenters(prev => [wc, ...prev]);
    setShowForm(false);
  };

  const handleDeactivate = async (id: string) => {
    try {
      await deactivateWorkCenter(tenantCode, id);
      setCenters(prev => prev.map(c => c.id === id ? { ...c, status: 'Inactive' } : c));
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível desativar o centro de trabalho.'));
    }
  };

  if (loading) return <Box sx={{ p: 6, textAlign: 'center' }}><CircularProgress size={32} /></Box>;
  if (error && centers.length === 0) return <PageError message={error} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader
        label="CENTROS DE TRABALHO"
        icon={<Factory size={20} />}
        action={
          <Authorized permission="production.write">
            <Button variant="contained" size="small" startIcon={<Plus size={16} />} onClick={() => setShowForm(v => !v)}>
              Novo Centro
            </Button>
          </Authorized>
        }
      />

      {error && <InlineError message={error} marginBottom={2} />}

      {showForm && (
        <CreateWorkCenterForm
          tenantCode={tenantCode}
          onCreated={handleCreated}
          onCancel={() => setShowForm(false)}
        />
      )}

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
                <TableCell>
                  <Chip
                    size="small"
                    label={wc.status === 'Active' ? 'Ativo' : 'Inativo'}
                    color={wc.status === 'Active' ? 'success' : 'default'}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>
                  {new Date(wc.createdAt).toLocaleDateString('pt-BR')}
                </TableCell>
                <TableCell align="right">
                  {wc.status === 'Active' && (
                    <Authorized permission="production.write">
                      <Tooltip title="Desativar">
                        <IconButton size="small" color="warning" onClick={() => void handleDeactivate(wc.id)}>
                          <PowerOff size={16} />
                        </IconButton>
                      </Tooltip>
                    </Authorized>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}

function CreateWorkCenterForm({ tenantCode, onCreated, onCancel }: {
  tenantCode: string;
  onCreated: (wc: WorkCenter) => void;
  onCancel: () => void;
}) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async () => {
    if (!code.trim() || !name.trim()) return;
    setSaving(true);
    setError(null);
    try {
      const wc = await createWorkCenter(tenantCode, { code: code.trim(), name: name.trim() });
      onCreated(wc);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível criar o centro de trabalho.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 3, mt: 2 }}>
      <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>NOVO CENTRO DE TRABALHO</Typography>
      {error && <InlineError message={error} marginBottom={2} />}
      <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
        <TextField
          label="Código"
          size="small"
          sx={{ width: 160 }}
          value={code}
          onChange={e => setCode(e.target.value.toUpperCase())}
          placeholder="SOLDA-01"
          slotProps={{ htmlInput: { style: { fontFamily: 'monospace', fontWeight: 700 } } }}
        />
        <TextField
          label="Nome"
          size="small"
          sx={{ flexGrow: 1 }}
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder="Linha de Soldagem 01"
        />
        <Button
          variant="contained"
          onClick={() => void handleSubmit()}
          disabled={saving || !code.trim() || !name.trim()}
          startIcon={saving ? <CircularProgress size={16} color="inherit" /> : undefined}
          sx={{ fontWeight: 800, whiteSpace: 'nowrap' }}
        >
          Criar
        </Button>
        <Button variant="outlined" onClick={onCancel} disabled={saving}>
          Cancelar
        </Button>
      </Stack>
    </Paper>
  );
}
