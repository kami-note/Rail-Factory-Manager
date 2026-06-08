import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions,
  DialogContent, DialogTitle, FormControl, IconButton, InputLabel,
  MenuItem, Paper, Select, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from '@mui/material';
import { Building2, CheckCircle2, Clock, Plus, Trash2, X } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { deleteTenant, getProvisionStatus, listTenants, registerTenant, type TenantSummary } from '../api/tenants';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };

const LOCALES = [
  { value: 'pt-BR', label: 'Português (Brasil)' },
  { value: 'en-US', label: 'English (US)' },
  { value: 'es-ES', label: 'Español' },
];

const TIMEZONES = [
  { value: 'America/Sao_Paulo', label: 'América/São Paulo (BRT)' },
  { value: 'America/Manaus', label: 'América/Manaus (AMT)' },
  { value: 'America/Fortaleza', label: 'América/Fortaleza (BRT-3)' },
  { value: 'UTC', label: 'UTC' },
];

function CreateTenantModal({ open, currentTenantCode, onCreated, onClose }: {
  open: boolean; currentTenantCode: string;
  onCreated: (t: TenantSummary) => void; onClose: () => void;
}) {
  const [code, setCode] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [locale, setLocale] = useState('pt-BR');
  const [timeZone, setTimeZone] = useState('America/Sao_Paulo');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setCode(''); setDisplayName(''); setLocale('pt-BR'); setTimeZone('America/Sao_Paulo'); setSaving(false); setError(null); }
  }, [open]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    setSaving(true); setError(null);
    try {
      const tenant = await registerTenant(currentTenantCode, { code, displayName, locale, timeZone });
      onCreated(tenant);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao criar tenant.'));
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="xs" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Novo Tenant
          <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField
              label="Código *"
              value={code}
              onChange={e => setCode(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
              fullWidth size="small" required autoFocus
              helperText="Letras minúsculas, números e hífens. Ex: minha-empresa"
              slotProps={{ htmlInput: { pattern: '[a-z0-9\\-]{2,50}' } }}
            />
            <TextField
              label="Nome / Razão Social *"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              fullWidth size="small" required
            />
            <FormControl fullWidth size="small">
              <InputLabel>Idioma</InputLabel>
              <Select value={locale} label="Idioma" onChange={e => setLocale(e.target.value)}>
                {LOCALES.map(l => <MenuItem key={l.value} value={l.value}>{l.label}</MenuItem>)}
              </Select>
            </FormControl>
            <FormControl fullWidth size="small">
              <InputLabel>Fuso Horário</InputLabel>
              <Select value={timeZone} label="Fuso Horário" onChange={e => setTimeZone(e.target.value)}>
                {TIMEZONES.map(t => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
              </Select>
            </FormControl>
            <Alert severity="info" sx={{ fontSize: 12 }}>
              Os bancos de dados serão criados automaticamente no Postgres e os schemas migrados em até 15 segundos após a criação.
            </Alert>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={saving || !code || !displayName}>
            {saving ? 'Criando...' : 'Criar Tenant'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}

function ProvisionBadge({ tenantCode }: { tenantCode: string }) {
  const [ready, setReady] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const poll = useCallback(async () => {
    try {
      const s = await getProvisionStatus(tenantCode);
      if (s.ready) {
        setReady(true);
        if (intervalRef.current) { clearInterval(intervalRef.current); intervalRef.current = null; }
      }
    } catch { /* retry */ }
  }, [tenantCode]);

  useEffect(() => {
    void poll();
    intervalRef.current = setInterval(() => { void poll(); }, 3000);
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, [poll]);

  if (ready) {
    return (
      <Tooltip title="Bancos de dados prontos">
        <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, color: 'success.main' }}>
          <CheckCircle2 size={14} />
          <Typography variant="caption" sx={{ fontWeight: 700 }}>Pronto</Typography>
        </Box>
      </Tooltip>
    );
  }
  return (
    <Tooltip title="Migrando bancos de dados...">
      <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 0.5, color: 'text.secondary' }}>
        <Clock size={14} />
        <Typography variant="caption">Provisionando</Typography>
      </Box>
    </Tooltip>
  );
}

function DeleteTenantDialog({ target, currentTenantCode, onDeleted, onClose }: {
  target: TenantSummary | null;
  currentTenantCode: string;
  onDeleted: (code: string) => void;
  onClose: () => void;
}) {
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!target) { setDeleting(false); setError(null); }
  }, [target]);

  const handleConfirm = async () => {
    if (!target || deleting) return;
    setDeleting(true); setError(null);
    try {
      await deleteTenant(currentTenantCode, target.code);
      onDeleted(target.code);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao remover tenant.'));
      setDeleting(false);
    }
  };

  return (
    <Dialog open={!!target} onClose={() => !deleting && onClose()} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
        Remover tenant
        <IconButton onClick={onClose} disabled={deleting} size="small"><X size={18} /></IconButton>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <Alert severity="warning">
            Esta ação é <strong>irreversível</strong>. Todos os 7 bancos de dados do tenant{' '}
            <strong>{target?.code}</strong> serão permanentemente excluídos.
          </Alert>
          <Typography variant="body2">
            Digite o código do tenant para confirmar:
          </Typography>
          <ConfirmCodeField expected={target?.code ?? ''} disabled={deleting} onMatch={handleConfirm} />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={deleting}>Cancelar</Button>
      </DialogActions>
    </Dialog>
  );
}

function ConfirmCodeField({ expected, disabled, onMatch }: {
  expected: string; disabled: boolean; onMatch: () => void;
}) {
  const [value, setValue] = useState('');
  useEffect(() => setValue(''), [expected]);
  const match = value === expected;
  return (
    <Stack spacing={1}>
      <TextField
        size="small" fullWidth
        value={value}
        onChange={e => setValue(e.target.value)}
        disabled={disabled}
        placeholder={expected}
        error={value.length > 0 && !match}
      />
      <Button
        variant="contained" color="error" fullWidth
        disabled={!match || disabled}
        onClick={onMatch}
      >
        {disabled ? 'Removendo...' : 'Confirmar exclusão'}
      </Button>
    </Stack>
  );
}

export function TenantManagementPage({ tenantCode }: Props) {
  const [tenants, setTenants] = useState<TenantSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<TenantSummary | null>(null);

  const load = async () => {
    setLoading(true); setFetchError(null);
    try { setTenants(await listTenants(tenantCode)); }
    catch (err) { setFetchError(toUiErrorMessage(err, 'Erro ao carregar tenants.')); }
    finally { setLoading(false); }
  };

  useEffect(() => { void load(); }, [tenantCode]);

  const handleCreated = (t: TenantSummary) => {
    setTenants(prev => [t, ...prev]);
    setCreateOpen(false);
    setSuccess(`Tenant "${t.code}" criado. Acompanhe o provisionamento na tabela abaixo.`);
  };

  const handleDeleted = (code: string) => {
    setTenants(prev => prev.filter(t => t.code !== code));
    setDeleteTarget(null);
    setSuccess(`Tenant "${code}" e todos os seus bancos de dados foram removidos.`);
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Gerenciamento de Tenants" icon={<Building2 size={20} />} />

      <Box sx={{ mb: 2, display: 'flex', justifyContent: 'flex-end' }}>
        <Button variant="contained" startIcon={<Plus size={16} />} onClick={() => setCreateOpen(true)}>
          Novo Tenant
        </Button>
      </Box>

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}

      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Código</TableCell>
              <TableCell>Nome</TableCell>
              <TableCell>Idioma</TableCell>
              <TableCell>Fuso</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Provisionamento</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {tenants.length === 0 && (
              <TableRow><TableCell colSpan={7} align="center" sx={{ py: 4, color: 'text.secondary' }}>Nenhum tenant cadastrado.</TableCell></TableRow>
            )}
            {tenants.map(t => (
              <TableRow key={t.code} hover>
                <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{t.code}</TableCell>
                <TableCell>{t.displayName}</TableCell>
                <TableCell>{t.locale}</TableCell>
                <TableCell sx={{ fontSize: 12, color: 'text.secondary' }}>{t.timeZone}</TableCell>
                <TableCell>
                  <Chip
                    label={t.status === 'Active' ? 'Ativo' : 'Inativo'}
                    color={t.status === 'Active' ? 'success' : 'default'}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  <ProvisionBadge key={t.code} tenantCode={t.code} />
                </TableCell>
                <TableCell align="right">
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => setDeleteTarget(t)}
                    title="Remover tenant"
                  >
                    <Trash2 size={15} />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <CreateTenantModal
        open={createOpen}
        currentTenantCode={tenantCode}
        onCreated={handleCreated}
        onClose={() => setCreateOpen(false)}
      />

      <DeleteTenantDialog
        target={deleteTarget}
        currentTenantCode={tenantCode}
        onDeleted={handleDeleted}
        onClose={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
