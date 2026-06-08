import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert, Box, Button, Card, CircularProgress, Container, FormControl, InputLabel,
  LinearProgress, MenuItem, Select, Stack, TextField, Typography, Link,
} from '@mui/material';
import { CheckCircle, Clock } from 'lucide-react';
import { toUiErrorMessage } from '../../shared/lib/http';
import { getProvisionStatus, type ProvisionStatus } from '../tenants/api/tenants';

const DB_LABELS: Record<string, string> = {
  iamdb: 'Usuários e Acessos',
  supplychaindb: 'Suprimentos',
  inventorydb: 'Estoque',
  productiondb: 'Produção',
  hrdb: 'Recursos Humanos',
  fleetdb: 'Frota',
  logisticsdb: 'Logística',
};
const DB_ORDER = ['iamdb', 'supplychaindb', 'inventorydb', 'productiondb', 'hrdb', 'fleetdb', 'logisticsdb'];

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

function ProvisioningScreen({ tenantCode }: { tenantCode: string }) {
  const [status, setStatus] = useState<ProvisionStatus | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const poll = useCallback(async () => {
    try {
      const s = await getProvisionStatus(tenantCode);
      setStatus(s);
      if (s.ready && intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    } catch { /* ignore, retry next tick */ }
  }, [tenantCode]);

  useEffect(() => {
    void poll();
    intervalRef.current = setInterval(() => { void poll(); }, 3000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [poll]);

  const dbs = DB_ORDER.filter(k => !status || k in status.databases);
  const readyCount = dbs.filter(k => status?.databases[k] === 'ready').length;
  const progress = dbs.length > 0 ? Math.round((readyCount / dbs.length) * 100) : 0;

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Card sx={{ p: 5, borderRadius: 3, border: 1, borderColor: 'divider', boxShadow: 0 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', mb: 2 }}>
          {status?.ready
            ? <CheckCircle size={48} color="#4caf50" />
            : <CircularProgress size={48} />}
        </Box>

        <Typography variant="h5" sx={{ fontWeight: 900, mb: 0.5, textAlign: 'center' }}>
          {status?.ready ? 'Plataforma pronta!' : 'Provisionando...'}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3, textAlign: 'center' }}>
          Tenant <strong>{tenantCode}</strong> — {readyCount}/{dbs.length} bancos prontos
        </Typography>

        <LinearProgress
          variant="determinate"
          value={progress}
          sx={{ mb: 3, height: 6, borderRadius: 3 }}
          color={status?.ready ? 'success' : 'primary'}
        />

        <Stack spacing={1} sx={{ mb: 4 }}>
          {dbs.map(key => {
            const dbStatus = status?.databases[key];
            const isReady = dbStatus === 'ready';
            return (
              <Box key={key} sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                {isReady
                  ? <CheckCircle size={16} color="#4caf50" />
                  : <Clock size={16} color="#bdbdbd" />}
                <Typography variant="body2" color={isReady ? 'text.primary' : 'text.secondary'}>
                  {DB_LABELS[key] ?? key}
                </Typography>
              </Box>
            );
          })}
        </Stack>

        <Button
          variant="contained"
          href="/"
          fullWidth
          size="large"
          disabled={!status?.ready}
          sx={{ py: 1.5, fontWeight: 900, borderRadius: 2 }}
        >
          {status?.ready ? 'Ir para o login' : 'Aguardando provisionamento...'}
        </Button>
      </Card>
    </Container>
  );
}

export function SetupPage() {
  const navigate = useNavigate();
  const [checking, setChecking] = useState(true);
  const [code, setCode] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [locale, setLocale] = useState('pt-BR');
  const [timeZone, setTimeZone] = useState('America/Sao_Paulo');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [createdCode, setCreatedCode] = useState('');

  useEffect(() => {
    fetch('/api/tenancy/tenants', { credentials: 'include' })
      .then(r => r.json())
      .then((tenants: unknown[]) => {
        if (tenants.length > 0) navigate('/', { replace: true });
        else setChecking(false);
      })
      .catch(() => setChecking(false));
  }, [navigate]);

  if (checking) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving) return;
    setSaving(true);
    setError(null);
    try {
      const response = await fetch('/api/tenancy/bootstrap', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code, displayName, locale, timeZone }),
        credentials: 'include',
      });
      if (!response.ok) {
        const body = await response.json().catch(() => ({})) as Record<string, unknown>;
        throw new Error(
          (body['detail'] as string) ?? (body['title'] as string) ?? `HTTP ${response.status}`
        );
      }
      setCreatedCode(code);
      setDone(true);
    } catch (err) {
      setError(toUiErrorMessage(err, 'Não foi possível configurar a plataforma.'));
      setSaving(false);
    }
  };

  if (done) {
    return <ProvisioningScreen tenantCode={createdCode} />;
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Card sx={{ p: 5, borderRadius: 3, border: 1, borderColor: 'divider', boxShadow: 0 }}>
        <Typography variant="h4" sx={{ fontWeight: 950, letterSpacing: '-0.05em', mb: 0.5 }}>
          Configuração inicial
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Crie o primeiro tenant da plataforma. Esta tela só está disponível antes do primeiro acesso.
        </Typography>

        <form onSubmit={handleSubmit}>
          <Stack spacing={2.5}>
            {error && <Alert severity="error">{error}</Alert>}

            <TextField
              label="Código da organização *"
              value={code}
              onChange={e => setCode(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
              fullWidth
              required
              helperText="Letras minúsculas, números e hífens. Ex: minha-empresa"
              slotProps={{ htmlInput: { pattern: '[a-z0-9\\-]{2,50}', minLength: 2 } }}
            />

            <TextField
              label="Nome / Razão Social *"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              fullWidth
              required
            />

            <FormControl fullWidth>
              <InputLabel>Idioma</InputLabel>
              <Select value={locale} label="Idioma" onChange={e => setLocale(e.target.value)}>
                {LOCALES.map(l => <MenuItem key={l.value} value={l.value}>{l.label}</MenuItem>)}
              </Select>
            </FormControl>

            <FormControl fullWidth>
              <InputLabel>Fuso Horário</InputLabel>
              <Select value={timeZone} label="Fuso Horário" onChange={e => setTimeZone(e.target.value)}>
                {TIMEZONES.map(t => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
              </Select>
            </FormControl>

            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              disabled={saving || !code || !displayName}
              sx={{ py: 1.5, fontWeight: 900, borderRadius: 2, mt: 1 }}
            >
              {saving ? 'Configurando...' : 'Configurar plataforma'}
            </Button>
          </Stack>
        </form>

        <Typography variant="body2" color="text.secondary" sx={{ mt: 3, textAlign: 'center' }}>
          <Link href="/" color="primary" underline="hover" sx={{ fontWeight: 700 }}>
            Voltar para o login
          </Link>
        </Typography>
      </Card>
    </Container>
  );
}
