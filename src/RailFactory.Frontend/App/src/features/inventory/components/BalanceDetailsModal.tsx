import React, { useEffect, useState } from 'react';
import { 
  Box, 
  Typography, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Paper, 
  CircularProgress,
  Divider,
  Stack,
  alpha,
  useTheme,
  Grid,
  Chip,
  Tooltip,
} from '@mui/material';
import {
  Factory as FactoryIcon,
  Package as PackageIcon,
  Hash as HashIcon,
  Boxes as BoxesIcon,
  ListChecks as ListChecksIcon,
  TrendingUp as TrendingUpIcon,
} from 'lucide-react';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { InlineError } from '../../../shared/components/common/InlineError';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

type BalanceDetailsModalProps = {
  balanceId: string | null;
  tenantCode: string;
  onClose: () => void;
};

type InventoryBalanceDetails = {
  id: string;
  materialCode: string;
  material: {
    materialCode: string;
    officialName: string;
    description: string;
    category: DisplayStatus;
    status: DisplayStatus;
    imageUrl?: string;
    ncm?: string;
    gtin?: string;
  };
  unitOfMeasure: string;
  status: DisplayStatus;
  createdAt: string;
  quantities: {
    totalPhysical: number;
    available: number;
    blocked: number;
    quarantine: number;
  };
  traceability: {
    lotNumber?: string;
    expirationDate?: string;
    sourceType: DisplayStatus;
    sourceReference: string;
    supplierName?: string;
    productionOrderId?: string;
    productionOrderNumber?: string;
  };
  ledger: Array<{
    occurredAt: string;
    quantityChange: number;
    newStatus: DisplayStatus;
    reason: string;
    user: string;
  }>;
};

/**
 * Maps internal ledger operation codes to Portuguese operator-facing labels.
 * @param reason - The internal operation code from the ledger.
 * @returns A human-readable Portuguese label.
 */
function formatLedgerReason(reason: string): string {
  const map: Record<string, string> = {
    pending_balance_created: 'Saldo Pendente Criado',
    balance_confirmed: 'Saldo Confirmado',
    production_consumed: 'Consumido em Produção',
    production_output: 'Saída de Produção',
    dispatch_debit: 'Débito de Expedição',
    reservation: 'Reserva para OP',
    reservation_released: 'Reserva Liberada',
    manual_adjustment: 'Ajuste Manual',
  };
  return map[reason] ?? reason;
}

/**
 * Calculates total produced quantity from ledger entries.
 */
function getTotalProduced(ledger: InventoryBalanceDetails['ledger']): number {
  return ledger
    .filter(e => e.reason === 'production_output' && e.quantityChange > 0)
    .reduce((sum, e) => sum + e.quantityChange, 0);
}

/**
 * Counts the number of production runs (distinct production_output events).
 */
function getProductionRunCount(ledger: InventoryBalanceDetails['ledger']): number {
  return ledger.filter(e => e.reason === 'production_output').length;
}

/**
 * Modal displaying full details and ledger for an Inventory Balance.
 * For FinishedGood (Production) items, renders an enriched Production section
 * showing the originating order number, total produced quantity, and run count.
 * @param props - Component properties.
 */
export function BalanceDetailsModal({ balanceId, tenantCode, onClose }: BalanceDetailsModalProps) {
  const theme = useTheme();
  const [details, setDetails] = useState<InventoryBalanceDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!balanceId) return;

    const fetchDetails = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchJsonOrThrow<InventoryBalanceDetails>(
          `/api/inventory/balances/${balanceId}`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Falha ao carregar detalhes do saldo'
        );
        setDetails(data);
      } catch (err) {
        setError(toUiErrorMessage(err, 'Não foi possível carregar os detalhes do saldo.'));
      } finally {
        setLoading(false);
      }
    };

    void fetchDetails();
  }, [balanceId, tenantCode]);

  const isProduction = details?.traceability.sourceType.key === 'Production';
  const totalProduced = details ? getTotalProduced(details.ledger) : 0;
  const runCount = details ? getProductionRunCount(details.ledger) : 0;

  return (
    <ResponsiveCenteredModal 
      open={!!balanceId} 
      title={`DETALHES DO ESTOQUE: ${details?.materialCode || '...'}`} 
      onClose={onClose}
    >
      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <InlineError message={error} />
      )}

      {details && !loading && (
        <Stack spacing={4}>
          {/* Main Info */}
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 6 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                <MaterialAvatar 
                  materialCode={details.materialCode} 
                  description={details.material.officialName}
                  imageUrl={details.material.imageUrl}
                  size={48} 
                />
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.2 }}>{details.material.officialName}</Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontWeight: 700, fontFamily: 'monospace' }}>
                    SKU: {details.materialCode}
                  </Typography>
                  {details.material.ncm && (
                    <Typography variant="caption" color="text.disabled" sx={{ display: 'block', fontFamily: 'monospace' }}>
                      NCM: {details.material.ncm}
                    </Typography>
                  )}
                </Box>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }} sx={{ textAlign: { md: 'right' } }}>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>CATEGORIA / STATUS</Typography>
              <Box sx={{ display: 'flex', justifyContent: { md: 'flex-end' }, gap: 1, mt: 0.5, flexWrap: 'wrap' }}>
                <StatusChip status={details.material.category} />
                <StatusChip status={details.status} />
              </Box>
            </Grid>
          </Grid>

          <Divider />

          {/* Stock Metrics */}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr 1fr' }, gap: 3 }}>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>SALDO FÍSICO</Typography>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.totalPhysical.toLocaleString('pt-BR')} <Typography component="span" variant="body2" color="text.secondary">{details.unitOfMeasure}</Typography></Typography>
            </Box>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>DISPONÍVEL</Typography>
              <Typography variant="h5" sx={{ fontWeight: 800, color: details.quantities.available > 0 ? 'success.main' : 'text.disabled' }}>
                {details.quantities.available.toLocaleString('pt-BR')} <Typography component="span" variant="body2" color="text.secondary">{details.unitOfMeasure}</Typography>
              </Typography>
            </Box>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>LOTE / VALIDADE</Typography>
              <Typography variant="body1" sx={{ fontWeight: 700 }}>{details.traceability.lotNumber || 'N/A'}</Typography>
              {details.traceability.expirationDate && (
                <Typography variant="caption" color="text.secondary">Vencimento: {formatRelativeDate(details.traceability.expirationDate, false)}</Typography>
              )}
            </Box>
          </Box>

          <Divider />

          {/* Production Section — only for FinishedGood/Production source */}
          {isProduction && (
            <>
              <Box
                sx={{
                  bgcolor: alpha(theme.palette.success.main, 0.05),
                  border: `1px solid ${alpha(theme.palette.success.main, 0.25)}`,
                  borderRadius: 2,
                  p: 2.5,
                }}
              >
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 2 }}>
                  <Box
                    sx={{
                      width: 36,
                      height: 36,
                      borderRadius: '50%',
                      bgcolor: alpha(theme.palette.success.main, 0.15),
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <FactoryIcon size={18} color={theme.palette.success.main} />
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" sx={{ fontWeight: 900, color: 'success.dark' }}>
                      INFORMAÇÕES DE PRODUÇÃO
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Produto acabado — gerado por ordens de produção internas
                    </Typography>
                  </Box>
                </Stack>

                <Grid container spacing={2}>
                  {/* Order Number */}
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                      <HashIcon size={16} color={theme.palette.text.secondary} style={{ marginTop: 2 }} />
                      <Box>
                        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>
                          ÚLTIMA ORDEM
                        </Typography>
                        {details.traceability.productionOrderNumber ? (
                          <Chip
                            label={details.traceability.productionOrderNumber}
                            size="small"
                            color="success"
                            variant="outlined"
                            sx={{ fontWeight: 800, fontFamily: 'monospace', mt: 0.5 }}
                          />
                        ) : (
                          <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.disabled' }}>N/A</Typography>
                        )}
                      </Box>
                    </Box>
                  </Grid>

                  {/* Total Produced */}
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                      <TrendingUpIcon size={16} color={theme.palette.text.secondary} style={{ marginTop: 2 }} />
                      <Box>
                        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>
                          TOTAL PRODUZIDO (HISTÓRICO)
                        </Typography>
                        <Typography variant="body1" sx={{ fontWeight: 800, color: 'success.main' }}>
                          {totalProduced.toLocaleString('pt-BR')} {details.unitOfMeasure}
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>

                  {/* Number of Production Runs */}
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                      <ListChecksIcon size={16} color={theme.palette.text.secondary} style={{ marginTop: 2 }} />
                      <Box>
                        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>
                          RODADAS DE PRODUÇÃO
                        </Typography>
                        <Typography variant="body1" sx={{ fontWeight: 800 }}>
                          {runCount} {runCount === 1 ? 'rodada' : 'rodadas'}
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>

                  {/* Current Stock */}
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                      <BoxesIcon size={16} color={theme.palette.text.secondary} style={{ marginTop: 2 }} />
                      <Box>
                        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>
                          SALDO ATUAL EM ESTOQUE
                        </Typography>
                        <Typography variant="body1" sx={{ fontWeight: 800, color: details.quantities.available > 0 ? 'success.main' : 'error.main' }}>
                          {details.quantities.available.toLocaleString('pt-BR')} {details.unitOfMeasure}
                        </Typography>
                      </Box>
                    </Box>
                  </Grid>
                </Grid>
              </Box>

              <Divider />
            </>
          )}

          {/* Origin Section — for non-production */}
          {!isProduction && (
            <>
              <Box>
                <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>ORIGEM / FORNECEDOR</Typography>
                <Box sx={{ mt: 0.5, display: 'flex', alignItems: 'center', gap: 1.5 }}>
                  <PackageIcon size={16} color={theme.palette.text.secondary} />
                  <StatusChip status={details.traceability.sourceType} label={details.traceability.supplierName || details.traceability.sourceType.label} />
                </Box>
                <Tooltip title="Clique para copiar referência">
                  <Typography
                    variant="caption"
                    color="text.disabled"
                    sx={{ display: 'block', mt: 0.5, fontFamily: 'monospace', cursor: 'pointer' }}
                    onClick={() => TechnicalIdFormatter.copyToClipboard(details.traceability.sourceReference)}
                  >
                    Ref: {TechnicalIdFormatter.truncate(details.traceability.sourceReference)}
                  </Typography>
                </Tooltip>
              </Box>
              <Divider />
            </>
          )}

          {/* Ledger / History */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>HISTÓRICO DE MOVIMENTAÇÕES</Typography>
            {details.ledger.length === 0 ? (
              <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                Sem movimentações registradas.
              </Typography>
            ) : (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ bgcolor: 'background.default' }}>
                      <TableCell sx={{ fontWeight: 800 }}>DATA</TableCell>
                      <TableCell align="right" sx={{ fontWeight: 800 }}>VARIAÇÃO</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>OPERAÇÃO</TableCell>
                      <TableCell sx={{ fontWeight: 800 }}>OPERADOR</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {details.ledger.map((entry, idx) => (
                      <TableRow key={idx} hover>
                        <TableCell>{formatRelativeDate(entry.occurredAt)}</TableCell>
                        <TableCell align="right">
                          <Typography 
                            variant="body2" 
                            sx={{ 
                              fontWeight: 700, 
                              color: entry.quantityChange >= 0 ? 'success.main' : 'error.main' 
                            }}
                          >
                            {entry.quantityChange > 0 ? `+${entry.quantityChange}` : entry.quantityChange}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <StatusChip status={entry.newStatus} />
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" sx={{ fontWeight: 600 }}>
                            {formatLedgerReason(entry.reason)}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption">{entry.user}</Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Box>

          <Box sx={{ bgcolor: alpha(theme.palette.primary.main, 0.03), p: 2, borderRadius: 1 }}>
             <Typography variant="caption" sx={{ display: 'block', opacity: 0.5, fontFamily: 'monospace' }}>
                ID Interno: {details.id} | Criado em: {formatRelativeDate(details.createdAt)}
              </Typography>
          </Box>
        </Stack>
      )}
    </ResponsiveCenteredModal>
  );
}
