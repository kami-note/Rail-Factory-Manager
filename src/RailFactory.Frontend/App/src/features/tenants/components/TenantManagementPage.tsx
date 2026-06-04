import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions,
  DialogContent, DialogTitle, FormControl, IconButton, InputLabel,
  MenuItem, Paper, Select, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { Building2, Plus, X } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { listTenants, registerTenant, type TenantSummary } from '../api/tenants';
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
              fullWidth size="small" required
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
              Os bancos de dados serão nomeados automaticamente como <code>tenant-{'{código}'}-iamdb</code>, <code>tenant-{'{código}'}-logisticsdb</code>, etc. Provisione-os no AppHost antes de ativar o tenant.
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

export function TenantManagementPage({ tenantCode }: Props) {
  const [tenants, setTenants] = useState<TenantSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

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
    setSuccess(`Tenant "${t.code}" criado. Provisione os bancos de dados no AppHost para ativá-lo.`);
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
              <TableCell>Bancos de dados</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {tenants.length === 0 && (
              <TableRow><TableCell colSpan={6} align="center" sx={{ py: 4, color: 'text.secondary' }}>Nenhum tenant cadastrado.</TableCell></TableRow>
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
                  <Typography variant="caption" color="text.secondary">
                    {Object.keys(t.connectionStrings).length} banco{Object.keys(t.connectionStrings).length !== 1 ? 's' : ''} configurado{Object.keys(t.connectionStrings).length !== 1 ? 's' : ''}
                  </Typography>
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
    </Box>
  );
}
