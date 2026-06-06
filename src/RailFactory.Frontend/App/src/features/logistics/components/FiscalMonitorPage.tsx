import React, { useMemo, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, InputAdornment,
  Pagination, Paper, Stack, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, TextField, Tooltip, Typography,
} from '@mui/material';
import { Copy, FileCheck2, RefreshCw, Search } from 'lucide-react';
import { ModuleHeader } from '../../../shared/components/common/ModuleHeader';
import { PageError } from '../../../shared/components/common/PageError';
import { Authorized } from '../../auth';
import { retryFiscalEmission } from '../api/logistics';
import { FiscalStatusCell } from './FiscalStatusCell';
import { toUiErrorMessage } from '../../../shared/lib/http';
import { RelativeDateFormatter, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { RETRYABLE_FISCAL_STATUSES } from '../types';
import { useFiscalDispatches } from '../hooks/useFiscalDispatches';
import {
  type FiscalFilterKey,
  FILTER_STATUSES, FILTER_LABELS, FILTER_COLORS,
} from '../hooks/fiscalFilters';
import type { Dispatch } from '../types';

type Props = { tenantCode: string };

const copyText = (text: string) => void TechnicalIdFormatter.copyToClipboard(text);

export function FiscalMonitorPage({ tenantCode }: Props) {
  const [filter, setFilter] = useState<FiscalFilterKey>('all');
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [retrying, setRetrying] = useState<string | null>(null);
  const [retryError, setRetryError] = useState<string | null>(null);
  const [retrySuccess, setRetrySuccess] = useState<string | null>(null);

  const { data, loading, error: fetchError, reload } = useFiscalDispatches(tenantCode, filter, page);

  const handleFilterChange = (key: FiscalFilterKey) => { setFilter(key); setPage(1); };

  const handleRetry = async (dispatch: Dispatch) => {
    setRetrying(dispatch.id);
    setRetryError(null);
    setRetrySuccess(null);
    try {
      await retryFiscalEmission(tenantCode, dispatch.id);
      setRetrySuccess(`Reemissão enfileirada para ${dispatch.trackingCode}. O status será atualizado em instantes.`);
      reload();
    } catch (err) {
      setRetryError(toUiErrorMessage(err, 'Erro ao enfileirar reemissão.'));
    } finally {
      setRetrying(null);
    }
  };

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return data?.items ?? [];
    return (data?.items ?? []).filter(d =>
      d.trackingCode.toLowerCase().includes(q)
      || (d.fiscalAccessKey ?? '').toLowerCase().includes(q)
      || (d.fiscalExternalId ?? '').toLowerCase().includes(q)
    );
  }, [data, search]);

  if (loading && !data) return <Box sx={{ p: 4 }}><CircularProgress /></Box>;
  if (fetchError) return <PageError message={fetchError} />;

  return (
    <Box sx={{ p: 3 }}>
      <ModuleHeader label="Monitor NF-e (Saída)" icon={<FileCheck2 size={20} />} />

      <Typography variant="body2" color="text.secondary" sx={{ mb: 3, maxWidth: 700 }}>
        Acompanhe o status de todas as NF-es emitidas na saída de mercadoria. Os dados são atualizados via webhooks do provider fiscal (PlugNotas / Focus NFe).
      </Typography>

      <Stack direction="row" spacing={1} sx={{ mb: 3, flexWrap: 'wrap', gap: 1 }}>
        {(Object.keys(FILTER_LABELS) as FiscalFilterKey[]).map(key => (
          <Chip
            key={key}
            label={key === 'all' ? `${FILTER_LABELS[key]} (${data?.total ?? 0})` : FILTER_LABELS[key]}
            color={filter === key ? FILTER_COLORS[key] : 'default'}
            variant={filter === key ? 'filled' : 'outlined'}
            onClick={() => handleFilterChange(key)}
            sx={{ fontWeight: 700, cursor: 'pointer' }}
          />
        ))}
      </Stack>

      {retrySuccess && <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }} onClose={() => setRetrySuccess(null)}>{retrySuccess}</Alert>}
      {retryError && <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }} onClose={() => setRetryError(null)}>{retryError}</Alert>}

      <TextField
        placeholder="Buscar por rastreio, chave de acesso ou ID externo..."
        value={search}
        onChange={e => setSearch(e.target.value)}
        size="small"
        fullWidth
        sx={{ mb: 2 }}
        slotProps={{
          input: {
            startAdornment: <InputAdornment position="start"><Search size={16} /></InputAdornment>,
            sx: { borderRadius: 2 },
          }
        }}
      />

      {filtered.length === 0 ? (
        <Alert severity="info" sx={{ borderRadius: 2 }}>
          Nenhuma NF-e encontrada para o filtro selecionado.
        </Alert>
      ) : (
        <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider', borderRadius: 2 }}>
          <Table size="small">
            <TableHead>
              <TableRow sx={{ bgcolor: 'grey.50' }}>
                <TableCell sx={{ fontWeight: 800 }}>Rastreio</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>Status / Chave NF-e</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>ID Externo (Provider)</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>Erro SEFAZ</TableCell>
                <TableCell sx={{ fontWeight: 800 }}>Despachado em</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {filtered.map(d => (
                <TableRow key={d.id} hover>
                  <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: 13 }}>
                    {d.trackingCode}
                  </TableCell>
                  <TableCell>
                    <FiscalStatusCell
                      status={d.fiscalStatus}
                      accessKey={d.fiscalAccessKey}
                      externalId={d.fiscalExternalId}
                      errorMessage={d.fiscalErrorMessage}
                    />
                  </TableCell>
                  <TableCell>
                    {d.fiscalExternalId ? (
                      <Tooltip title="Copiar ID Externo">
                        <Box
                          onClick={() => copyText(d.fiscalExternalId!)}
                          sx={{ display: 'flex', alignItems: 'center', gap: 0.5, cursor: 'pointer', color: 'text.secondary', '&:hover': { color: 'primary.main' } }}
                        >
                          <Typography variant="caption" sx={{ fontFamily: 'monospace', fontSize: 11 }}>
                            {d.fiscalExternalId}
                          </Typography>
                          <Copy size={10} />
                        </Box>
                      </Tooltip>
                    ) : <span style={{ color: '#bbb', fontSize: 12 }}>—</span>}
                  </TableCell>
                  <TableCell>
                    {d.fiscalErrorMessage ? (
                      <Tooltip title={d.fiscalErrorMessage}>
                        <Typography
                          variant="caption"
                          color="error"
                          sx={{ cursor: 'help', maxWidth: 220, display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                        >
                          {d.fiscalErrorMessage}
                        </Typography>
                      </Tooltip>
                    ) : <span style={{ color: '#bbb', fontSize: 12 }}>—</span>}
                  </TableCell>
                  <TableCell sx={{ color: 'text.secondary', fontSize: 12 }}>
                    {RelativeDateFormatter.format(d.dispatchedAt)}
                  </TableCell>
                  <TableCell align="right">
                    {RETRYABLE_FISCAL_STATUSES.has(d.fiscalStatus ?? '') && (
                      <Authorized permission="logistics.fiscal">
                        <Tooltip title="Reemitir NF-e — enfileira nova tentativa via provider fiscal">
                          <Button
                            size="small"
                            variant="outlined"
                            color="warning"
                            startIcon={retrying === d.id ? <CircularProgress size={12} color="inherit" /> : <RefreshCw size={13} />}
                            disabled={retrying === d.id}
                            onClick={() => handleRetry(d)}
                            sx={{ fontWeight: 700, fontSize: 11, whiteSpace: 'nowrap' }}
                          >
                            Reemitir
                          </Button>
                        </Tooltip>
                      </Authorized>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {(data?.totalPages ?? 0) > 1 && (
        <Box sx={{ mt: 3, display: 'flex', justifyContent: 'center' }}>
          <Pagination
            count={data?.totalPages ?? 1}
            page={page}
            onChange={(_, p) => setPage(p)}
            color="primary"
            shape="rounded"
          />
        </Box>
      )}
    </Box>
  );
}
