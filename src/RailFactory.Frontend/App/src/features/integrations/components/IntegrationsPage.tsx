import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Divider,
  IconButton, Paper, Stack, Switch, Tooltip, Typography,
} from '@mui/material';
import { Plug, Plus, Settings2 } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { listIntegrations, enableIntegration, disableIntegration } from '../api/integrations';
import { ConfigureIntegrationModal } from './ConfigureIntegrationModal';
import { CATEGORY_LABELS, CATEGORY_PROVIDERS, PROVIDER_SCHEMAS, type Integration, type IntegrationCategory } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };

const ALL_CATEGORIES = Object.keys(CATEGORY_LABELS) as IntegrationCategory[];

export function IntegrationsPage({ tenantCode }: Props) {
  const [integrations, setIntegrations] = useState<Integration[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [configuring, setConfiguring] = useState<{ category: string; existing?: Integration } | null>(null);
  const [toggling, setToggling] = useState<string | null>(null);

  const load = async () => {
    setLoading(true); setFetchError(null);
    try {
      const data = await listIntegrations(tenantCode);
      setIntegrations(data);
    } catch (err) {
      setFetchError(toUiErrorMessage(err, 'Erro ao carregar integrações.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [tenantCode]);

  const handleToggle = async (integration: Integration) => {
    setToggling(integration.category);
    setMutationError(null);
    try {
      const updated = integration.isEnabled
        ? await disableIntegration(tenantCode, integration.category)
        : await enableIntegration(tenantCode, integration.category);
      setIntegrations(prev => prev.map(i => i.id === updated.id ? updated : i));
      setSuccess(`${CATEGORY_LABELS[integration.category] ?? integration.category} ${updated.isEnabled ? 'habilitado' : 'desabilitado'}.`);
    } catch (err) {
      setMutationError(toUiErrorMessage(err, 'Erro ao alterar status.'));
    } finally {
      setToggling(null);
    }
  };

  const handleSaved = () => {
    setConfiguring(null);
    setSuccess('Integração salva com sucesso.');
    void load();
  };

  if (loading) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  const byCategory = new Map(integrations.map(i => [i.category, i]));

  return (
    <Box sx={{ p: 3, maxWidth: 860 }}>
      <ModuleHeader label="Integrações" icon={<Plug size={20} />} />

      {success && <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      <Stack spacing={2}>
        {ALL_CATEGORIES.map(category => {
          const integration = byCategory.get(category);
          const isTogglingThis = toggling === category;

          return (
            <Paper key={category} elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 2, p: 2.5 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="subtitle1" fontWeight={700}>
                    {CATEGORY_LABELS[category]}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
                    {integration
                      ? `${PROVIDER_SCHEMAS[integration.providerType]?.label ?? integration.providerType} — atualizado ${new Date(integration.updatedAt).toLocaleDateString('pt-BR')}`
                      : 'Não configurado'}
                  </Typography>
                </Box>

                <Stack direction="row" spacing={1} alignItems="center">
                  {integration && (
                    <Tooltip title={integration.isEnabled ? 'Desabilitar' : 'Habilitar'}>
                      <Switch
                        checked={integration.isEnabled}
                        disabled={isTogglingThis}
                        onChange={() => handleToggle(integration)}
                        size="small"
                      />
                    </Tooltip>
                  )}

                  {integration && (
                    <Chip
                      label={integration.isEnabled ? 'Ativo' : 'Inativo'}
                      color={integration.isEnabled ? 'success' : 'default'}
                      size="small"
                      variant="outlined"
                    />
                  )}

                  <Tooltip title={integration ? 'Editar configuração' : 'Configurar'}>
                    <IconButton
                      size="small"
                      onClick={() => setConfiguring({ category, existing: integration })}
                    >
                      {integration ? <Settings2 size={16} /> : <Plus size={16} />}
                    </IconButton>
                  </Tooltip>
                </Stack>
              </Box>

              {integration && (
                <>
                  <Divider sx={{ my: 1.5 }} />
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label={`Provider: ${PROVIDER_SCHEMAS[integration.providerType]?.label ?? integration.providerType}`} size="small" variant="outlined" />
                    <Chip label={`Categoria: ${category}`} size="small" variant="outlined" />
                  </Stack>
                </>
              )}
            </Paper>
          );
        })}
      </Stack>

      {configuring && (
        <ConfigureIntegrationModal
          open={!!configuring}
          tenantCode={tenantCode}
          category={configuring.category}
          existingProviderType={configuring.existing?.providerType}
          onSaved={handleSaved}
          onClose={() => setConfiguring(null)}
        />
      )}
    </Box>
  );
}
