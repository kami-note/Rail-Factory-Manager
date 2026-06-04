import React, { useEffect, useState } from 'react';
import {
  Box, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControl, Grid, IconButton, InputLabel, MenuItem, Select,
  Tab, Tabs, TextField, Typography, Avatar
} from '@mui/material';
import { X, Plug, ShieldAlert } from 'lucide-react';
import { configureIntegration } from '../api/integrations';
import {
  CATEGORY_LABELS, CATEGORY_PROVIDERS, PROVIDER_SCHEMAS, PROVIDER_METADATA,
  type CredentialField, type IntegrationCategory,
} from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = {
  open: boolean;
  tenantCode: string;
  category: IntegrationCategory | string;
  existingProviderType?: string;
  onSaved: () => void;
  onClose: () => void;
};

function CredentialFieldInput({
  field, value, onChange,
}: { field: CredentialField; value: string; onChange: (v: string) => void }) {
  return (
    <Grid xs={12} sm={field.key.startsWith('emitter_state') || field.key.startsWith('emitter_number') ? 4 : 12}>
      <TextField
        label={field.label}
        value={value}
        onChange={e => onChange(e.target.value)}
        fullWidth
        size="small"
        required={field.required}
        placeholder={field.placeholder}
        type={field.secret ? 'password' : 'text'}
        helperText={field.hint}
        slotProps={{ htmlInput: { maxLength: field.key === 'emitter_state' ? 2 : undefined } }}
      />
    </Grid>
  );
}

export function ConfigureIntegrationModal({ open, tenantCode, category, existingProviderType, onSaved, onClose }: Props) {
  const [providerType, setProviderType] = useState(existingProviderType ?? '');
  const [tab, setTab] = useState(0);
  const [credentials, setCredentials] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const providers = CATEGORY_PROVIDERS[category] ?? [];
  const schema = PROVIDER_SCHEMAS[providerType];

  // Reset all state when modal closes
  useEffect(() => {
    if (!open) {
      setProviderType(existingProviderType ?? '');
      setCredentials({});
      setTab(0);
      setSaving(false);
      setError(null);
    }
  }, [open, existingProviderType]);

  // Only clear credentials/tab when the user actively switches provider AFTER the modal is open
  const handleProviderChange = (newProvider: string) => {
    if (newProvider !== providerType) {
      setCredentials({});
      setTab(0);
    }
    setProviderType(newProvider);
  };

  const setField = (key: string, value: string) =>
    setCredentials(prev => ({ ...prev, [key]: value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving || !providerType) return;
    setSaving(true); setError(null);
    try {
      const filtered = Object.fromEntries(
        Object.entries(credentials).filter(([, v]) => v.trim() !== '')
      );
      await configureIntegration(tenantCode, { category, providerType, credentials: filtered });
      onSaved();
    } catch (err) {
      setError(toUiErrorMessage(err, 'Erro ao salvar integração.'));
      setSaving(false);
    }
  };

  const renderFields = (fields: CredentialField[]) => (
    <Grid container spacing={1.5} sx={{ mt: 0.5 }}>
      {fields.map(f => (
        <CredentialFieldInput
          key={f.key}
          field={f}
          value={credentials[f.key] ?? ''}
          onChange={v => setField(f.key, v)}
        />
      ))}
    </Grid>
  );

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 800 }}>
          Configurar — {CATEGORY_LABELS[category] ?? category}
          <IconButton onClick={onClose} disabled={saving} size="small"><X size={18} /></IconButton>
        </DialogTitle>

        <DialogContent>
          <Box sx={{ mb: 3, display: 'flex', gap: 2, alignItems: 'center', bgcolor: 'grey.50', p: 2, borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
            <Avatar 
              src={providerType && providerType !== 'mock' && PROVIDER_METADATA[providerType] ? `https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=http://${PROVIDER_METADATA[providerType].domain}&size=128` : undefined}
              variant="rounded"
              sx={{ width: 48, height: 48, bgcolor: 'white', border: '1px solid', borderColor: 'divider', p: 0.5, '& img': { objectFit: 'contain' } }}
            >
              {!providerType && <Plug size={24} color="#999" />}
              {providerType === 'mock' && <ShieldAlert size={24} color="#666" />}
            </Avatar>
            <FormControl fullWidth size="small" required sx={{ bgcolor: 'white' }}>
              <InputLabel>Selecione o Provedor</InputLabel>
              <Select
                value={providerType}
                label="Selecione o Provedor"
                onChange={e => handleProviderChange(e.target.value)}
              >
                {providers.map(p => (
                  <MenuItem key={p} value={p}>
                    {PROVIDER_SCHEMAS[p]?.label ?? p}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>

          {error && <Typography color="error" variant="body2" sx={{ mb: 1 }}>{error}</Typography>}

          {schema && schema.providerType !== 'mock' && (
            <>
              <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}>
                <Tab label="Credenciais" />
                <Tab label="Emitente" />
                <Tab label="Webhook" />
              </Tabs>

              {tab === 0 && renderFields(schema.credentialFields)}
              {tab === 1 && renderFields(schema.emitterFields)}
              {tab === 2 && (
                <>
                  {renderFields(schema.webhookFields)}
                  {schema.webhookFields.length > 0 && (
                    <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                      Configure a URL do webhook no painel do provider apontando para:{' '}
                      <code>/api/logistics/webhooks/{providerType}/{'{tenantCode}'}</code>
                    </Typography>
                  )}
                </>
              )}
            </>
          )}

          {schema?.providerType === 'mock' && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              O provider Mock simula respostas sem chamadas externas. Nenhuma credencial necessária.
            </Typography>
          )}
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button type="submit" variant="contained" disabled={saving || !providerType}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
