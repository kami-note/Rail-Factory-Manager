import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, Divider,
  Grid, Paper, Stack, TextField, Typography,
} from '@mui/material';
import { Save, Settings2 } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { SnackbarAlert } from '../../../shared/components/common/SnackbarAlert';
import { useFiscalProfile } from '../hooks/useFiscalProfile';
import { upsertFiscalProfile } from '../api/logistics';
import { toUiErrorMessage } from '../../../shared/lib/http';
import type { TenantFiscalProfile } from '../types';

type Props = { tenantCode: string };

type FormState = Omit<TenantFiscalProfile, 'updatedAt'>;

const DEFAULTS: FormState = {
  cfopPadraoIntraestadual: '5102',
  cfopPadraoInterestadual: '6102',
  ufOrigem: '',
  icmsRate: 12,
  icmsCst: '40',
  pisCst: '07',
  cofinsCst: '07',
  ipiRate: 0,
  icmsOrigin: 0,
};

function profileToForm(p: TenantFiscalProfile): FormState {
  return {
    cfopPadraoIntraestadual: p.cfopPadraoIntraestadual,
    cfopPadraoInterestadual: p.cfopPadraoInterestadual,
    ufOrigem: p.ufOrigem,
    icmsRate: p.icmsRate,
    icmsCst: p.icmsCst,
    pisCst: p.pisCst,
    cofinsCst: p.cofinsCst,
    ipiRate: p.ipiRate,
    icmsOrigin: p.icmsOrigin,
  };
}

export function FiscalSettingsPage({ tenantCode }: Props) {
  const { data: profile, loading, error: fetchError } = useFiscalProfile(tenantCode);
  const [form, setForm] = useState<FormState>(DEFAULTS);
  const [saving, setSaving] = useState(false);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (profile) setForm(profileToForm(profile));
  }, [profile]);

  const set = (field: keyof FormState) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.type === 'number' ? parseFloat(e.target.value) || 0 : e.target.value;
    setForm(prev => ({ ...prev, [field]: val }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMutationError(null);
    try {
      await upsertFiscalProfile(tenantCode, form);
      setSuccess('Perfil fiscal salvo com sucesso.');
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao salvar perfil fiscal.'));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;

  return (
    <Box sx={{ p: 3, maxWidth: 700 }}>
      <ModuleHeader label="Configurações Fiscais" icon={<Settings2 size={20} />} />

      {fetchError && <Alert severity="error" sx={{ mb: 2 }}>{fetchError}</Alert>}

      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Defina os padrões fiscais do tenant. Esses valores são usados como padrão ao criar itens de expedição,
        reduzindo a entrada manual de dados nas notas fiscais.
      </Typography>

      <Paper elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 3 }}>
        <form onSubmit={handleSubmit}>
          <Stack spacing={3}>

            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>CFOP Padrão</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 6 }}>
                  <TextField
                    label="CFOP Intraestadual"
                    value={form.cfopPadraoIntraestadual}
                    onChange={set('cfopPadraoIntraestadual')}
                    fullWidth size="small"
                    helperText="Ex: 5102 (venda dentro do estado)"
                    slotProps={{ htmlInput: { maxLength: 5 } }}
                  />
                </Grid>
                <Grid size={{ xs: 6 }}>
                  <TextField
                    label="CFOP Interestadual"
                    value={form.cfopPadraoInterestadual}
                    onChange={set('cfopPadraoInterestadual')}
                    fullWidth size="small"
                    helperText="Ex: 6102 (venda para outro estado)"
                    slotProps={{ htmlInput: { maxLength: 5 } }}
                  />
                </Grid>
              </Grid>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>Localização</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 3 }}>
                  <TextField
                    label="UF de Origem"
                    value={form.ufOrigem}
                    onChange={set('ufOrigem')}
                    fullWidth size="small"
                    helperText="Ex: SP, MG, RS"
                    slotProps={{ htmlInput: { maxLength: 2, style: { textTransform: 'uppercase' } } }}
                  />
                </Grid>
              </Grid>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>ICMS</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Alíquota ICMS (%)"
                    value={form.icmsRate}
                    onChange={set('icmsRate')}
                    fullWidth size="small" type="number"
                    slotProps={{ htmlInput: { step: 0.01, min: 0, max: 100 } }}
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="CST ICMS"
                    value={form.icmsCst}
                    onChange={set('icmsCst')}
                    fullWidth size="small"
                    helperText="Ex: 40 (isento)"
                    slotProps={{ htmlInput: { maxLength: 3 } }}
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Origem ICMS"
                    value={form.icmsOrigin}
                    onChange={set('icmsOrigin')}
                    fullWidth size="small" type="number"
                    helperText="0 = nacional"
                    slotProps={{ htmlInput: { min: 0, max: 8 } }}
                  />
                </Grid>
              </Grid>
            </Stack>

            <Divider />

            <Stack spacing={1}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>PIS / COFINS / IPI</Typography>
              <Grid container spacing={2}>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="CST PIS"
                    value={form.pisCst}
                    onChange={set('pisCst')}
                    fullWidth size="small"
                    helperText="Ex: 07 (isento)"
                    slotProps={{ htmlInput: { maxLength: 3 } }}
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="CST COFINS"
                    value={form.cofinsCst}
                    onChange={set('cofinsCst')}
                    fullWidth size="small"
                    helperText="Ex: 07 (isento)"
                    slotProps={{ htmlInput: { maxLength: 3 } }}
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Alíquota IPI (%)"
                    value={form.ipiRate}
                    onChange={set('ipiRate')}
                    fullWidth size="small" type="number"
                    slotProps={{ htmlInput: { step: 0.01, min: 0, max: 100 } }}
                  />
                </Grid>
              </Grid>
            </Stack>

            <Divider />

            <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="submit"
                variant="contained"
                disabled={saving}
                startIcon={saving ? <CircularProgress size={14} color="inherit" /> : <Save size={16} />}
              >
                {saving ? 'Salvando...' : 'Salvar Configurações'}
              </Button>
            </Box>

          </Stack>
        </form>
      </Paper>

      <SnackbarAlert message={success} severity="success" onClose={() => setSuccess(null)} />
      <SnackbarAlert message={mutationError} severity="error" onClose={() => setMutationError(null)} duration={6000} />
    </Box>
  );
}
