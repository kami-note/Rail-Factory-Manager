import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Divider,
  IconButton, Paper, Stack, Switch, Tooltip, Typography, Grid, Avatar
} from '@mui/material';
import { Plug, Plus, Settings2, ExternalLink, ShieldAlert, CheckCircle2, Copy, Link } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { listIntegrations, enableIntegration, disableIntegration } from '../api/integrations';
import { ConfigureIntegrationModal } from './ConfigureIntegrationModal';
import { CATEGORY_LABELS, CATEGORY_PROVIDERS, PROVIDER_SCHEMAS, PROVIDER_METADATA, type Integration, type IntegrationCategory } from '../types';
import { toUiErrorMessage } from '../../../shared/lib/http';

type Props = { tenantCode: string };

const ALL_CATEGORIES = Object.keys(CATEGORY_LABELS) as IntegrationCategory[];

function WebhookUrlBox({ tenantCode, category, providerType }: { tenantCode: string; category: string; providerType: string }) {
  const [copied, setCopied] = useState(false);
  const baseUrl = window.location.origin;
  const serviceMap: Record<string, string> = { fiscal: 'logistics', payment: 'logistics', shipping: 'logistics' };
  const service = serviceMap[category] ?? category;
  const webhookUrl = `${baseUrl}/api/${service}/webhooks/${providerType}/${tenantCode}`;

  const handleCopy = () => {
    void navigator.clipboard.writeText(webhookUrl).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  return (
    <Box
      sx={{
        p: 1.5,
        borderRadius: 1.5,
        bgcolor: 'grey.50',
        border: '1px dashed',
        borderColor: 'divider',
      }}
    >
      <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', mb: 0.5 }}>
        <Link size={12} color="#888" />
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.04em', fontSize: '0.6rem' }}>
          URL do Webhook (configure no painel do provider)
        </Typography>
      </Stack>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Typography
          variant="caption"
          sx={{
            fontFamily: 'monospace',
            fontSize: 10,
            color: 'text.primary',
            wordBreak: 'break-all',
            flexGrow: 1,
            lineHeight: 1.4,
          }}
        >
          {webhookUrl}
        </Typography>
        <Tooltip title={copied ? 'Copiado!' : 'Copiar URL'}>
          <IconButton size="small" onClick={handleCopy} sx={{ flexShrink: 0, color: copied ? 'success.main' : 'text.secondary' }}>
            {copied ? <CheckCircle2 size={14} /> : <Copy size={14} />}
          </IconButton>
        </Tooltip>
      </Stack>
    </Box>
  );
}

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
    <Box sx={{ p: 3, maxWidth: 1200 }}>
      <ModuleHeader label="Serviços e Integrações" icon={<Plug size={24} />} />

      <Typography variant="body1" color="text.secondary" sx={{ mb: 4, maxWidth: 800 }}>
        Gerencie as conexões do sistema com provedores externos. Configure tokens de API, webhooks e habilite ou desabilite integrações rapidamente.
      </Typography>

      {success && <Alert severity="success" sx={{ mb: 3 }} onClose={() => setSuccess(null)}>{success}</Alert>}
      {mutationError && <Alert severity="error" sx={{ mb: 3 }} onClose={() => setMutationError(null)}>{mutationError}</Alert>}

      <Grid container spacing={3}>
        {ALL_CATEGORIES.map(category => {
          const integration = byCategory.get(category);
          const isTogglingThis = toggling === category;
          const providerType = integration?.providerType;
          const meta = providerType ? PROVIDER_METADATA[providerType] : null;
          const providerLabel = providerType ? (PROVIDER_SCHEMAS[providerType]?.label ?? providerType) : null;

          return (
            <Grid item xs={12} md={6} lg={4} key={category}>
              <Paper 
                elevation={0} 
                sx={{ 
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  border: '1px solid', 
                  borderColor: integration?.isEnabled ? 'primary.main' : 'divider',
                  borderRadius: 3, 
                  overflow: 'hidden',
                  transition: 'all 0.2s',
                  boxShadow: integration?.isEnabled ? '0 4px 12px rgba(0,0,0,0.05)' : 'none',
                  '&:hover': {
                    borderColor: 'primary.main',
                    boxShadow: '0 4px 12px rgba(0,0,0,0.08)'
                  }
                }}
              >
                {/* Header Section */}
                <Box sx={{ p: 3, display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', borderBottom: '1px solid', borderColor: 'divider', bgcolor: integration?.isEnabled ? 'rgba(0,0,0,0.01)' : 'transparent' }}>
                  <Stack direction="row" spacing={2} alignItems="center">
                    <Avatar 
                      src={meta && providerType !== 'mock' ? `https://t3.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=http://${meta.domain}&size=128` : undefined}
                      variant="rounded"
                      sx={{ 
                        width: 48, 
                        height: 48, 
                        bgcolor: integration ? 'white' : 'grey.100',
                        color: 'grey.400',
                        border: '1px solid',
                        borderColor: 'divider',
                        p: 0.5,
                        '& img': { objectFit: 'contain' }
                      }}
                    >
                      {!integration && <Plug size={24} />}
                      {integration && providerType === 'mock' && <ShieldAlert size={24} color="#666" />}
                    </Avatar>
                    <Box>
                      <Typography variant="subtitle2" color="primary.main" fontWeight={800} sx={{ textTransform: 'uppercase', letterSpacing: '0.05em', fontSize: '0.65rem' }}>
                        {CATEGORY_LABELS[category]}
                      </Typography>
                      <Typography variant="h6" fontWeight={700} sx={{ lineHeight: 1.2, mt: 0.5 }}>
                        {providerLabel || 'Não Configurado'}
                      </Typography>
                    </Box>
                  </Stack>

                  {integration && (
                    <Tooltip title={integration.isEnabled ? 'Desabilitar' : 'Habilitar'}>
                      <Switch
                        checked={integration.isEnabled}
                        disabled={isTogglingThis}
                        onChange={() => handleToggle(integration)}
                        color="success"
                      />
                    </Tooltip>
                  )}
                </Box>

                {/* Body Section */}
                <Box sx={{ p: 3, flexGrow: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    {integration 
                      ? `Esta integração está configurada para conectar o sistema Rail Factory com o provedor ${providerLabel}.`
                      : `Nenhum provedor selecionado para a categoria de ${CATEGORY_LABELS[category].toLowerCase()}. Clique em configurar para adicionar.`}
                  </Typography>

                  {integration && (
                    <Stack spacing={1.5} sx={{ mt: 'auto' }}>
                      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                        <Chip
                          icon={integration.isEnabled ? <CheckCircle2 size={14} /> : undefined}
                          label={integration.isEnabled ? 'Sincronização Ativa' : 'Pausada'}
                          color={integration.isEnabled ? 'success' : 'default'}
                          size="small"
                          sx={{ fontWeight: 600 }}
                        />
                        <Chip label={`Atualizado: ${new Date(integration.updatedAt).toLocaleDateString('pt-BR')}`} size="small" variant="outlined" />
                      </Box>
                      {(category === 'fiscal' || category === 'payment' || category === 'shipping') && providerType && providerType !== 'mock' && (
                        <WebhookUrlBox tenantCode={tenantCode} category={category} providerType={providerType} />
                      )}
                    </Stack>
                  )}
                </Box>

                {/* Footer Section */}
                <Box sx={{ p: 2, px: 3, borderTop: '1px solid', borderColor: 'divider', display: 'flex', justifyContent: 'space-between', alignItems: 'center', bgcolor: 'grey.50' }}>
                  {integration && meta && meta.docUrl !== '#' ? (
                    <Button 
                      size="small" 
                      startIcon={<ExternalLink size={14} />} 
                      href={meta.docUrl} 
                      target="_blank"
                      sx={{ color: 'text.secondary', fontWeight: 600 }}
                    >
                      Documentação
                    </Button>
                  ) : (
                    <Box /> // Spacer
                  )}

                  <Button
                    variant={integration ? "outlined" : "contained"}
                    size="small"
                    startIcon={integration ? <Settings2 size={16} /> : <Plus size={16} />}
                    onClick={() => setConfiguring({ category, existing: integration })}
                    sx={{ fontWeight: 700, borderRadius: 2 }}
                  >
                    {integration ? 'Configurar' : 'Conectar'}
                  </Button>
                </Box>
              </Paper>
            </Grid>
          );
        })}
      </Grid>

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
