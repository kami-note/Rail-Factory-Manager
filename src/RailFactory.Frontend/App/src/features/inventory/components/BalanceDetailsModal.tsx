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
  Accordion,
  AccordionSummary,
  AccordionDetails,
} from '@mui/material';
import {
  Factory as FactoryIcon,
  Package as PackageIcon,
  Hash as HashIcon,
  Boxes as BoxesIcon,
  ListChecks as ListChecksIcon,
  TrendingUp as TrendingUpIcon,
  ChevronDown as ChevronDownIcon,
  ClipboardList as ClipboardListIcon,
  Layers as LayersIcon,
  CheckCircle2 as CheckCircleIcon,
  XCircle as XCircleIcon,
  Clock as ClockIcon,
  PlayCircle as PlayCircleIcon,
} from 'lucide-react';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { InlineError } from '../../../shared/components/common/InlineError';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

// ─── Types ───────────────────────────────────────────────────────────────────

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

type BomItem = {
  id: string;
  materialCode: string;
  quantity: number;
  unitOfMeasure: string;
  scrapFactor: number;
};

type BomSummary = {
  id: string;
  productCode: string;
  version: number;
  status: DisplayStatus;
  batchSize: number;
  items: BomItem[];
  createdAt: string;
  updatedAt: string;
};

type ProductionOrderSummary = {
  id: string;
  orderNumber: string;
  productCode: string;
  bomId: string;
  workCenterId: string;
  plannedQuantity: number;
  status: DisplayStatus;
  createdAt: string;
  updatedAt: string;
};

// ─── Helpers ─────────────────────────────────────────────────────────────────

/**
 * Maps internal ledger operation codes to Portuguese operator-facing labels.
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

function getTotalProduced(ledger: InventoryBalanceDetails['ledger']): number {
  return ledger
    .filter(e => e.reason === 'production_output' && e.quantityChange > 0)
    .reduce((sum, e) => sum + e.quantityChange, 0);
}

function getProductionRunCount(ledger: InventoryBalanceDetails['ledger']): number {
  return ledger.filter(e => e.reason === 'production_output').length;
}

/**
 * Returns icon and color for a production order status chip.
 */
function OrderStatusIcon({ statusKey }: { statusKey: string }) {
  switch (statusKey) {
    case 'Completed': return <CheckCircleIcon size={14} />;
    case 'Cancelled': return <XCircleIcon size={14} />;
    case 'InExecution': return <PlayCircleIcon size={14} />;
    default: return <ClockIcon size={14} />;
  }
}

// ─── Component ───────────────────────────────────────────────────────────────

/**
 * Modal displaying full details and ledger for an Inventory Balance.
 *
 * For FinishedGood (Production) items, renders two additional sections:
 *  1. Active BOM structure (component list with quantities and scrap factors)
 *  2. Production order history for this product (filterable by productCode)
 *
 * @remarks
 * Data sources:
 *  - `/api/inventory/balances/{id}` — base balance + ledger
 *  - `/api/production/boms?productCode=XXX` — BOM structure (Production API)
 *  - `/api/production/production-orders?productCode=XXX` — order history (Production API)
 */
export function BalanceDetailsModal({ balanceId, tenantCode, onClose }: BalanceDetailsModalProps) {
  const theme = useTheme();

  const [details, setDetails] = useState<InventoryBalanceDetails | null>(null);
  const [boms, setBoms] = useState<BomSummary[]>([]);
  const [orders, setOrders] = useState<ProductionOrderSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [bomLoading, setBomLoading] = useState(false);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch base inventory details
  useEffect(() => {
    if (!balanceId) return;

    const fetchDetails = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchJsonOrThrow<InventoryBalanceDetails>(
          `/api/inventory/balances/${balanceId}`,
          { headers: buildTenantHeaders(tenantCode), credentials: 'include' },
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

  // Fetch BOM + Production Order history once we know the materialCode and it's a FinishedGood
  useEffect(() => {
    if (!details || details.traceability.sourceType.key !== 'Production') return;

    const code = details.materialCode;
    const headers = { ...buildTenantHeaders(tenantCode), credentials: 'include' as const };

    const fetchBoms = async () => {
      setBomLoading(true);
      try {
        const data = await fetchJsonOrThrow<BomSummary[]>(
          `/api/production/boms?productCode=${encodeURIComponent(code)}`,
          { headers: buildTenantHeaders(tenantCode), credentials: 'include' },
          'Falha ao carregar BOMs'
        );
        setBoms(data);
      } catch {
        setBoms([]);
      } finally {
        setBomLoading(false);
      }
    };

    const fetchOrders = async () => {
      setOrdersLoading(true);
      try {
        const data = await fetchJsonOrThrow<ProductionOrderSummary[]>(
          `/api/production/production-orders?productCode=${encodeURIComponent(code)}`,
          { headers: buildTenantHeaders(tenantCode), credentials: 'include' },
          'Falha ao carregar ordens'
        );
        setOrders(data);
      } catch {
        setOrders([]);
      } finally {
        setOrdersLoading(false);
      }
    };

    void fetchBoms();
    void fetchOrders();
    // Suppress lint: 'headers' is constructed inline intentionally
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [details?.materialCode, details?.traceability.sourceType.key]);

  const isProduction = details?.traceability.sourceType.key === 'Production';
  const totalProduced = details ? getTotalProduced(details.ledger) : 0;
  const runCount = details ? getProductionRunCount(details.ledger) : 0;
  const activeBom = boms.find(b => b.status.key === 'Active') ?? boms[0] ?? null;

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

      {error && <InlineError message={error} />}

      {details && !loading && (
        <Stack spacing={3}>

          {/* ── Material Header ─────────────────────────────────────── */}
          <Grid container spacing={2} sx={{ alignItems: 'flex-start' }}>
            <Grid size={{ xs: 12, md: 7 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <MaterialAvatar
                  materialCode={details.materialCode}
                  description={details.material.officialName}
                  imageUrl={details.material.imageUrl}
                  size={52}
                />
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800, lineHeight: 1.2 }}>
                    {details.material.officialName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontWeight: 700, fontFamily: 'monospace' }}>
                    SKU: {details.materialCode}
                  </Typography>
                  {details.material.description && (
                    <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.25 }}>
                      {details.material.description}
                    </Typography>
                  )}
                </Box>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 5 }} sx={{ textAlign: { md: 'right' } }}>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>CATEGORIA / STATUS</Typography>
              <Box sx={{ display: 'flex', justifyContent: { md: 'flex-end' }, gap: 1, mt: 0.5, flexWrap: 'wrap' }}>
                <StatusChip status={details.material.category} />
                <StatusChip status={details.status} />
              </Box>
              {(details.material.ncm || details.material.gtin) && (
                <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 1, fontFamily: 'monospace' }}>
                  {details.material.ncm && `NCM: ${details.material.ncm}`}
                  {details.material.ncm && details.material.gtin && ' · '}
                  {details.material.gtin && `GTIN: ${details.material.gtin}`}
                </Typography>
              )}
            </Grid>
          </Grid>

          <Divider />

          {/* ── Stock Quantities ────────────────────────────────────── */}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr 1fr', sm: '1fr 1fr 1fr' }, gap: 2 }}>
            <Box sx={{ p: 2, bgcolor: alpha(theme.palette.success.main, 0.06), borderRadius: 1.5, border: `1px solid ${alpha(theme.palette.success.main, 0.2)}` }}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>DISPONÍVEL</Typography>
              <Typography variant="h5" sx={{ fontWeight: 900, color: 'success.main' }}>
                {details.quantities.available.toLocaleString('pt-BR')}
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>{details.unitOfMeasure}</Typography>
              </Typography>
            </Box>
            <Box sx={{ p: 2, bgcolor: alpha(theme.palette.primary.main, 0.04), borderRadius: 1.5, border: `1px solid ${alpha(theme.palette.primary.main, 0.15)}` }}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>FÍSICO TOTAL</Typography>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>
                {details.quantities.totalPhysical.toLocaleString('pt-BR')}
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 0.5 }}>{details.unitOfMeasure}</Typography>
              </Typography>
            </Box>
            <Box sx={{ p: 2, bgcolor: alpha(theme.palette.grey[500], 0.04), borderRadius: 1.5, border: `1px solid ${alpha(theme.palette.grey[500], 0.15)}`, gridColumn: { xs: '1/-1', sm: 'auto' } }}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>LOTE / VALIDADE</Typography>
              <Typography variant="body1" sx={{ fontWeight: 700 }}>{details.traceability.lotNumber || '—'}</Typography>
              {details.traceability.expirationDate && (
                <Typography variant="caption" color="text.secondary">Vence: {formatRelativeDate(details.traceability.expirationDate, false)}</Typography>
              )}
            </Box>
          </Box>

          <Divider />

          {/* ── Production Section (FinishedGood only) ─────────────── */}
          {isProduction && (
            <>
              {/* KPI strip */}
              <Box
                sx={{
                  bgcolor: alpha(theme.palette.success.main, 0.05),
                  border: `1px solid ${alpha(theme.palette.success.main, 0.2)}`,
                  borderRadius: 2,
                  p: 2,
                }}
              >
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 2 }}>
                  <Box sx={{ width: 34, height: 34, borderRadius: '50%', bgcolor: alpha(theme.palette.success.main, 0.15), display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <FactoryIcon size={18} color={theme.palette.success.main} />
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" sx={{ fontWeight: 900, color: 'success.dark' }}>PRODUÇÃO INTERNA</Typography>
                    <Typography variant="caption" color="text.secondary">Produto acabado gerado por ordens de produção</Typography>
                  </Box>
                </Stack>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 6, sm: 3 }}>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.25 }}>
                      <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>ÚLTIMA ORDEM</Typography>
                      {details.traceability.productionOrderNumber ? (
                        <Chip label={details.traceability.productionOrderNumber} size="small" color="success" variant="outlined" sx={{ fontWeight: 800, fontFamily: 'monospace', width: 'fit-content', mt: 0.25 }} />
                      ) : (
                        <Typography variant="body2" color="text.disabled">—</Typography>
                      )}
                    </Box>
                  </Grid>
                  <Grid size={{ xs: 6, sm: 3 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>TOTAL PRODUZIDO</Typography>
                    <Typography variant="body1" sx={{ fontWeight: 800, color: 'success.main' }}>{totalProduced.toLocaleString('pt-BR')} {details.unitOfMeasure}</Typography>
                  </Grid>
                  <Grid size={{ xs: 6, sm: 3 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>RODADAS</Typography>
                    <Typography variant="body1" sx={{ fontWeight: 800 }}>{runCount}</Typography>
                  </Grid>
                  <Grid size={{ xs: 6, sm: 3 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block' }}>ORDENS REGISTRADAS</Typography>
                    <Typography variant="body1" sx={{ fontWeight: 800 }}>{ordersLoading ? '...' : orders.length}</Typography>
                  </Grid>
                </Grid>
              </Box>

              {/* ── BOM Structure ──────────────────────────────────── */}
              <Accordion defaultExpanded disableGutters elevation={0} sx={{ border: `1px solid ${theme.palette.divider}`, borderRadius: 2, '&:before': { display: 'none' }, overflow: 'hidden' }}>
                <AccordionSummary
                  expandIcon={<ChevronDownIcon size={18} />}
                  sx={{ bgcolor: alpha(theme.palette.primary.main, 0.04), minHeight: 48, '& .MuiAccordionSummary-content': { alignItems: 'center', gap: 1.5 } }}
                >
                  <LayersIcon size={18} color={theme.palette.primary.main} />
                  <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>ESTRUTURA DE BOM</Typography>
                  {activeBom && (
                    <Chip label={`v${activeBom.version} · ${activeBom.status.label}`} size="small" color="primary" variant="outlined" sx={{ fontWeight: 700, fontSize: '0.7rem' }} />
                  )}
                </AccordionSummary>
                <AccordionDetails sx={{ p: 0 }}>
                  {bomLoading ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                      <CircularProgress size={28} />
                    </Box>
                  ) : !activeBom ? (
                    <Box sx={{ p: 3, textAlign: 'center' }}>
                      <Typography variant="body2" color="text.disabled" sx={{ fontStyle: 'italic' }}>
                        Nenhum BOM encontrado para este produto.
                      </Typography>
                    </Box>
                  ) : (
                    <>
                      <Box sx={{ px: 2, py: 1.5, bgcolor: alpha(theme.palette.grey[500], 0.04), borderBottom: `1px solid ${theme.palette.divider}` }}>
                        <Typography variant="caption" color="text.secondary">
                          Tamanho do lote: <strong>{activeBom.batchSize} {details.unitOfMeasure}</strong>
                          {' · '}
                          Versão <strong>v{activeBom.version}</strong>
                          {' · '}
                          {activeBom.items.length} componente{activeBom.items.length !== 1 ? 's' : ''}
                        </Typography>
                      </Box>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow sx={{ bgcolor: 'background.default' }}>
                              <TableCell sx={{ fontWeight: 800 }}>COMPONENTE</TableCell>
                              <TableCell align="right" sx={{ fontWeight: 800 }}>QUANTIDADE</TableCell>
                              <TableCell sx={{ fontWeight: 800 }}>UN</TableCell>
                              <TableCell align="right" sx={{ fontWeight: 800 }}>SCRAP %</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {activeBom.items.map(item => (
                              <TableRow key={item.id} hover>
                                <TableCell>
                                  <Typography variant="caption" sx={{ fontWeight: 700, fontFamily: 'monospace', color: 'primary.main' }}>
                                    {item.materialCode}
                                  </Typography>
                                </TableCell>
                                <TableCell align="right" sx={{ fontWeight: 700 }}>{item.quantity.toLocaleString('pt-BR')}</TableCell>
                                <TableCell sx={{ fontWeight: 600 }}>{item.unitOfMeasure}</TableCell>
                                <TableCell align="right">
                                  {item.scrapFactor > 0 ? (
                                    <Chip label={`${(item.scrapFactor * 100).toFixed(1)}%`} size="small" color="warning" variant="outlined" sx={{ fontWeight: 700, fontSize: '0.7rem' }} />
                                  ) : (
                                    <Typography variant="caption" color="text.disabled">—</Typography>
                                  )}
                                </TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                      {boms.length > 1 && (
                        <Box sx={{ px: 2, py: 1, bgcolor: alpha(theme.palette.grey[500], 0.03), borderTop: `1px solid ${theme.palette.divider}` }}>
                          <Typography variant="caption" color="text.disabled">
                            {boms.length - 1} versão(ões) inativa(s) não exibida(s).
                          </Typography>
                        </Box>
                      )}
                    </>
                  )}
                </AccordionDetails>
              </Accordion>

              {/* ── Production Order History ─────────────────────── */}
              <Accordion defaultExpanded disableGutters elevation={0} sx={{ border: `1px solid ${theme.palette.divider}`, borderRadius: 2, '&:before': { display: 'none' }, overflow: 'hidden' }}>
                <AccordionSummary
                  expandIcon={<ChevronDownIcon size={18} />}
                  sx={{ bgcolor: alpha(theme.palette.primary.main, 0.04), minHeight: 48, '& .MuiAccordionSummary-content': { alignItems: 'center', gap: 1.5 } }}
                >
                  <ClipboardListIcon size={18} color={theme.palette.primary.main} />
                  <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>HISTÓRICO DE ORDENS DE PRODUÇÃO</Typography>
                  <Chip label={ordersLoading ? '...' : orders.length} size="small" sx={{ fontWeight: 700 }} />
                </AccordionSummary>
                <AccordionDetails sx={{ p: 0 }}>
                  {ordersLoading ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                      <CircularProgress size={28} />
                    </Box>
                  ) : orders.length === 0 ? (
                    <Box sx={{ p: 3, textAlign: 'center' }}>
                      <Typography variant="body2" color="text.disabled" sx={{ fontStyle: 'italic' }}>
                        Nenhuma ordem de produção encontrada para este produto.
                      </Typography>
                    </Box>
                  ) : (
                    <TableContainer>
                      <Table size="small">
                        <TableHead>
                          <TableRow sx={{ bgcolor: 'background.default' }}>
                            <TableCell sx={{ fontWeight: 800 }}>N° ORDEM</TableCell>
                            <TableCell align="right" sx={{ fontWeight: 800 }}>QTD PLANEJADA</TableCell>
                            <TableCell sx={{ fontWeight: 800 }}>STATUS</TableCell>
                            <TableCell sx={{ fontWeight: 800 }}>CRIADA EM</TableCell>
                          </TableRow>
                        </TableHead>
                        <TableBody>
                          {orders.map(order => (
                            <TableRow key={order.id} hover>
                              <TableCell>
                                <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                                  <OrderStatusIcon statusKey={order.status.key} />
                                  <Typography variant="caption" sx={{ fontWeight: 800, fontFamily: 'monospace' }}>
                                    {order.orderNumber}
                                  </Typography>
                                </Stack>
                              </TableCell>
                              <TableCell align="right" sx={{ fontWeight: 700 }}>
                                {order.plannedQuantity.toLocaleString('pt-BR')} {details.unitOfMeasure}
                              </TableCell>
                              <TableCell>
                                <StatusChip status={order.status} />
                              </TableCell>
                              <TableCell>
                                <Typography variant="caption" color="text.secondary">
                                  {formatRelativeDate(order.createdAt)}
                                </Typography>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  )}
                </AccordionDetails>
              </Accordion>

              <Divider />
            </>
          )}

          {/* ── Origin Section (non-production) ────────────────────── */}
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

          {/* ── Ledger / Movimentações ──────────────────────────────── */}
          <Accordion disableGutters elevation={0} sx={{ border: `1px solid ${theme.palette.divider}`, borderRadius: 2, '&:before': { display: 'none' }, overflow: 'hidden' }}>
            <AccordionSummary
              expandIcon={<ChevronDownIcon size={18} />}
              sx={{ bgcolor: alpha(theme.palette.primary.main, 0.04), minHeight: 48, '& .MuiAccordionSummary-content': { alignItems: 'center', gap: 1.5 } }}
            >
              <BoxesIcon size={18} color={theme.palette.primary.main} />
              <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>HISTÓRICO DE MOVIMENTAÇÕES</Typography>
              <Chip label={details.ledger.length} size="small" sx={{ fontWeight: 700 }} />
            </AccordionSummary>
            <AccordionDetails sx={{ p: 0 }}>
              {details.ledger.length === 0 ? (
                <Box sx={{ p: 3, textAlign: 'center' }}>
                  <Typography variant="body2" color="text.disabled" sx={{ fontStyle: 'italic' }}>
                    Sem movimentações registradas.
                  </Typography>
                </Box>
              ) : (
                <TableContainer>
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
                              sx={{ fontWeight: 700, color: entry.quantityChange >= 0 ? 'success.main' : 'error.main' }}
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
            </AccordionDetails>
          </Accordion>

          {/* ── Footer ─────────────────────────────────────────────── */}
          <Box sx={{ bgcolor: alpha(theme.palette.primary.main, 0.03), p: 1.5, borderRadius: 1 }}>
            <Typography variant="caption" sx={{ display: 'block', opacity: 0.5, fontFamily: 'monospace' }}>
              ID: {details.id} · Criado em: {formatRelativeDate(details.createdAt)}
            </Typography>
          </Box>
        </Stack>
      )}
    </ResponsiveCenteredModal>
  );
}
