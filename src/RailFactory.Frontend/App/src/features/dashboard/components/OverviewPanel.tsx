import React from 'react';
import {
  Box,
  Button,
  CircularProgress,
  Grid,
  LinearProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
  useMediaQuery,
  useTheme,
  Chip,
} from '@mui/material';
import { ArrowUpRight, ShieldCheck, Package, Factory, CheckCircle, Trash2, Timer, Settings, BarChart2 } from 'lucide-react';
import type { Status } from '../types';
import { StatCard } from '../../../shared/components/common/StatCard';
import { useProductionDashboard } from '../hooks/useProductionDashboard';
import { useInventoryDashboard } from '../hooks/useInventoryDashboard';

/**
 * Renders the main dashboard overview panel with real KPI data.
 *
 * @param status - Current system status (tenant, environment).
 * @param statusError - Error from the status fetch, if any.
 * @param tenantCode - Used to fetch dashboard KPIs.
 * @param onNavigate - Navigation callback for quick action buttons.
 *
 * @remarks
 * Invariant (BFF-Driven): All KPIs are computed by the backend and consumed here read-only.
 * The frontend never derives aggregate numbers from raw domain lists.
 */
export function OverviewPanel({
  status,
  statusError,
  tenantCode,
  onNavigate,
}: {
  status: Status | null;
  statusError: string | null;
  tenantCode: string;
  onNavigate: (path: string) => void;
}) {
  const theme = useTheme();
  const isSmall = useMediaQuery(theme.breakpoints.down('sm'));

  const { data: production, loading: prodLoading } = useProductionDashboard(tenantCode);
  const { data: inventory, loading: invLoading } = useInventoryDashboard(tenantCode);

  const activeOrders = production?.activeOrders ?? null;
  const availableCount = inventory?.availableCount ?? null;
  const passRate = production?.inspectionSummary?.passRate ?? null;
  const topScrap = production?.topScrap ?? [];
  const ordersByStatus = production?.ordersByStatus ?? {};
  const avgLeadTime = production?.averageLeadTimeHours ?? null;
  const workCenterSummary = production?.workCenterSummary ?? [];
  const stockAccuracy = inventory?.stockAccuracy ?? null;
  const blockedCount = inventory?.blockedCount ?? null;

  const fmt = (n: number | null, loading: boolean) =>
    loading ? '…' : n !== null ? String(n) : '--';

  const fmtPct = (n: number | null, loading: boolean) =>
    loading ? '…' : n !== null ? `${(n * 100).toFixed(1)}%` : '--';

  const fmtLeadTime = (hours: number | null, loading: boolean) => {
    if (loading) return '…';
    if (hours === null) return '--';
    if (hours < 1) return `${Math.round(hours * 60)}min`;
    if (hours < 24) return `${hours.toFixed(1)}h`;
    return `${(hours / 24).toFixed(1)}d`;
  };

  const passRateColor =
    passRate === null ? 'text.secondary'
    : passRate >= 0.9 ? 'success.main'
    : passRate >= 0.7 ? 'warning.main'
    : 'error.main';

  const statusLabels: Record<string, { label: string; color: 'default' | 'info' | 'warning' | 'success' | 'error' }> = {
    Draft: { label: 'Rascunho', color: 'default' },
    Released: { label: 'Liberada', color: 'info' },
    InExecution: { label: 'Em Execução', color: 'warning' },
    Completed: { label: 'Concluída', color: 'success' },
    Cancelled: { label: 'Cancelada', color: 'error' },
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', bgcolor: '#ffffff' }}>
      {/* KPI STRIP */}
      <Box sx={{ borderBottom: '1px solid #edebe9' }}>
        <Grid container>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { sm: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', md: 0 } }}>
            <StatCard
              label="ORDENS ATIVAS"
              value={fmt(activeOrders, prodLoading)}
              icon={prodLoading ? <CircularProgress size={14} /> : <Factory size={16} />}
              color="info.main"
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { md: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', sm: 0 } }}>
            <StatCard
              label="SALDOS DISPONÍVEIS"
              value={fmt(availableCount, invLoading)}
              icon={invLoading ? <CircularProgress size={14} /> : <Package size={16} />}
              color="success.main"
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }} sx={{ borderRight: { sm: '1px solid #f3f2f1' }, borderBottom: { xs: '1px solid #f3f2f1', md: 0 } }}>
            <StatCard
              label="TAXA DE APROVAÇÃO"
              value={fmtPct(passRate, prodLoading)}
              icon={prodLoading ? <CircularProgress size={14} /> : <CheckCircle size={16} />}
              color={passRateColor}
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, md: 3 }}>
            <StatCard
              label="LEAD TIME MÉDIO"
              value={fmtLeadTime(avgLeadTime, prodLoading)}
              icon={prodLoading ? <CircularProgress size={14} /> : <Timer size={16} />}
              color="text.secondary"
            />
          </Grid>
        </Grid>
      </Box>

      {/* QUICK ACTIONS — tablet/desktop only */}
      {!isSmall && (
        <Box sx={{ px: 4, py: 2, display: 'flex', justifyContent: 'space-between', bgcolor: '#faf9f8', borderBottom: '1px solid #edebe9', gap: 2, alignItems: 'center' }}>
          <Typography variant="caption" color="text.secondary">
            Tenant: <strong>{status?.tenant.code.toUpperCase() ?? '—'}</strong>
            {statusError && (
              <Box component="span" sx={{ ml: 2, color: 'error.main' }}>⚠ Erro de conexão</Box>
            )}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button
              variant="outlined"
              disableElevation
              onClick={() => onNavigate('/app/production/orders')}
              sx={{ height: 32, px: 3, fontSize: '0.75rem', fontWeight: 800, borderRadius: 2 }}
              endIcon={<ArrowUpRight size={14} />}
            >
              Nova Ordem
            </Button>
            <Button
              variant="contained"
              disableElevation
              onClick={() => onNavigate('/app/receipts')}
              sx={{ height: 32, px: 3, fontSize: '0.75rem', fontWeight: 800, borderRadius: 2 }}
              endIcon={<ArrowUpRight size={14} />}
            >
              Gerenciar Recebimentos
            </Button>
          </Stack>
        </Box>
      )}

      {/* MAIN WORKSPACE */}
      <Box sx={{ flexGrow: 1, overflowY: 'auto', p: { xs: 2, md: 4 } }}>
        <Grid container spacing={4}>

          {/* LEFT COLUMN */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Stack spacing={3}>
              {/* TOP SCRAP */}
              <Box sx={{ border: '1px solid #edebe9', borderRadius: 2, overflow: 'hidden' }}>
                <Box sx={{ px: 3, py: 2, borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Trash2 size={14} color={theme.palette.error.main} />
                  <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
                    Top Scrap por Material
                  </Typography>
                </Box>
                {prodLoading ? (
                  <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
                    <CircularProgress size={20} />
                  </Box>
                ) : topScrap.length === 0 ? (
                  <Box sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Nenhum scrap registrado.
                    </Typography>
                  </Box>
                ) : (
                  <Table size="small">
                    <TableHead>
                      <TableRow sx={{ bgcolor: '#faf9f8' }}>
                        <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem', textTransform: 'uppercase' }}>Material</TableCell>
                        <TableCell align="right" sx={{ fontWeight: 700, fontSize: '0.7rem', textTransform: 'uppercase' }}>Qtd. Scrap</TableCell>
                        <TableCell sx={{ fontWeight: 700, fontSize: '0.7rem', textTransform: 'uppercase' }}>UM</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {topScrap.map((item) => (
                        <TableRow key={item.materialCode} hover>
                          <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.8rem', fontWeight: 600 }}>
                            {item.materialCode}
                          </TableCell>
                          <TableCell align="right" sx={{ color: 'error.main', fontWeight: 700 }}>
                            {item.totalScrap.toLocaleString('pt-BR', { maximumFractionDigits: 3 })}
                          </TableCell>
                          <TableCell sx={{ color: 'text.secondary', fontSize: '0.75rem' }}>
                            {item.unitOfMeasure}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                )}
              </Box>

              {/* ACURACIDADE DE ESTOQUE */}
              <Box sx={{ border: '1px solid #edebe9', borderRadius: 2, overflow: 'hidden' }}>
                <Box sx={{ px: 3, py: 2, borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 1 }}>
                  <BarChart2 size={14} color={theme.palette.primary.main} />
                  <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
                    Acuracidade de Estoque
                  </Typography>
                </Box>
                {invLoading ? (
                  <Box sx={{ p: 3, display: 'flex', justifyContent: 'center' }}><CircularProgress size={20} /></Box>
                ) : stockAccuracy === null ? (
                  <Box sx={{ p: 3, textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Nenhuma conferência concluída.
                    </Typography>
                  </Box>
                ) : (
                  <Box sx={{ p: 3 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                      <Typography variant="body2" color="text.secondary">
                        Disponíveis vs. Bloqueados
                      </Typography>
                      <Typography variant="body2" sx={{ fontWeight: 800, color: stockAccuracy >= 0.95 ? 'success.main' : stockAccuracy >= 0.8 ? 'warning.main' : 'error.main' }}>
                        {(stockAccuracy * 100).toFixed(1)}%
                      </Typography>
                    </Box>
                    <LinearProgress
                      variant="determinate"
                      value={Math.round(stockAccuracy * 100)}
                      sx={{
                        height: 8,
                        borderRadius: 4,
                        bgcolor: '#fde7e9',
                        mb: 1.5,
                        '& .MuiLinearProgress-bar': {
                          bgcolor: stockAccuracy >= 0.95 ? '#107c10' : stockAccuracy >= 0.8 ? '#ff8c00' : '#d13438',
                          borderRadius: 4,
                        },
                      }}
                    />
                    <Stack direction="row" spacing={3}>
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 800, color: 'success.main' }}>
                          {availableCount ?? '--'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">Disponíveis</Typography>
                      </Box>
                      <Box>
                        <Typography variant="h6" sx={{ fontWeight: 800, color: 'error.main' }}>
                          {blockedCount ?? '--'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">Bloqueados</Typography>
                      </Box>
                    </Stack>
                  </Box>
                )}
              </Box>

              {/* INSPEÇÃO SUMMARY */}
              {(production || prodLoading) && (
                <Box sx={{ border: '1px solid #edebe9', borderRadius: 2, overflow: 'hidden' }}>
                  <Box sx={{ px: 3, py: 2, borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 1 }}>
                    <CheckCircle size={14} color={theme.palette.success.main} />
                    <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
                      Inspeções de Qualidade
                    </Typography>
                  </Box>
                  {prodLoading ? (
                    <Box sx={{ p: 3 }}><CircularProgress size={20} /></Box>
                  ) : (
                    <Stack direction="row" divider={<Box sx={{ borderRight: '1px solid #f3f2f1' }} />}>
                      <Box sx={{ flex: 1, p: 2, textAlign: 'center' }}>
                        <Typography variant="h5" sx={{ fontWeight: 800, color: 'success.main' }}>
                          {production!.inspectionSummary.passed}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                          Aprovadas
                        </Typography>
                      </Box>
                      <Box sx={{ flex: 1, p: 2, textAlign: 'center' }}>
                        <Typography variant="h5" sx={{ fontWeight: 800, color: 'error.main' }}>
                          {production!.inspectionSummary.failed}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                          Reprovadas
                        </Typography>
                      </Box>
                      <Box sx={{ flex: 1, p: 2, textAlign: 'center' }}>
                        <Typography variant="h5" sx={{ fontWeight: 800, color: passRateColor }}>
                          {(production!.inspectionSummary.passRate * 100).toFixed(1)}%
                        </Typography>
                        <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase' }}>
                          Taxa
                        </Typography>
                      </Box>
                    </Stack>
                  )}
                </Box>
              )}
            </Stack>
          </Grid>

          {/* RIGHT COLUMN */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Stack spacing={3}>
              {/* ORDENS POR STATUS */}
              <Box sx={{ border: '1px solid #edebe9', borderRadius: 2, overflow: 'hidden' }}>
                <Box sx={{ px: 3, py: 2, borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Factory size={14} color={theme.palette.info.main} />
                  <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
                    Ordens por Status
                  </Typography>
                </Box>
                {prodLoading ? (
                  <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
                    <CircularProgress size={20} />
                  </Box>
                ) : Object.keys(ordersByStatus).length === 0 ? (
                  <Box sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Nenhuma ordem registrada.
                    </Typography>
                  </Box>
                ) : (
                  <Stack spacing={0} divider={<Box sx={{ borderBottom: '1px solid #f3f2f1' }} />}>
                    {Object.entries(ordersByStatus)
                      .map(([s, count]) => {
                        const meta = statusLabels[s] ?? { label: s, color: 'default' as const };
                        return (
                          <Box key={s} sx={{ px: 3, py: 1.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center', opacity: count === 0 ? 0.4 : 1 }}>
                            <Chip label={meta.label} color={count > 0 ? meta.color : 'default'} size="small" sx={{ fontWeight: 700, fontSize: '0.7rem' }} />
                            <Typography variant="h6" sx={{ fontWeight: 800, color: count === 0 ? 'text.disabled' : 'text.primary' }}>{count}</Typography>
                          </Box>
                        );
                      })}
                  </Stack>
                )}
              </Box>

              {/* DESEMPENHO POR WORK CENTER */}
              <Box sx={{ border: '1px solid #edebe9', borderRadius: 2, overflow: 'hidden' }}>
                <Box sx={{ px: 3, py: 2, borderBottom: '1px solid #edebe9', display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Settings size={14} color={theme.palette.primary.main} />
                  <Typography variant="caption" sx={{ fontWeight: 800, textTransform: 'uppercase', color: 'text.secondary' }}>
                    Desempenho por Centro de Trabalho
                  </Typography>
                </Box>
                {prodLoading ? (
                  <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
                    <CircularProgress size={20} />
                  </Box>
                ) : workCenterSummary.length === 0 ? (
                  <Box sx={{ p: 4, textAlign: 'center' }}>
                    <Typography variant="body2" color="text.secondary">
                      Nenhum centro de trabalho com ordens.
                    </Typography>
                  </Box>
                ) : (
                  <Stack spacing={0} divider={<Box sx={{ borderBottom: '1px solid #f3f2f1' }} />}>
                    {workCenterSummary.map((wc) => {
                      const pct = Math.round(wc.completionRate * 100);
                      const barColor = pct >= 80 ? '#107c10' : pct >= 50 ? '#ff8c00' : '#d13438';
                      return (
                        <Box key={wc.workCenterId} sx={{ px: 3, py: 2 }}>
                          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                            <Box>
                              <Typography variant="body2" sx={{ fontWeight: 700 }}>
                                {wc.workCenterName}
                              </Typography>
                              <Typography variant="caption" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                                {wc.workCenterCode}
                              </Typography>
                            </Box>
                            <Box sx={{ textAlign: 'right' }}>
                              <Typography variant="body2" sx={{ fontWeight: 800, color: barColor }}>
                                {pct}%
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {wc.completedOrders}/{wc.totalOrders} OPs
                              </Typography>
                            </Box>
                          </Box>
                          <LinearProgress
                            variant="determinate"
                            value={pct}
                            sx={{
                              height: 4,
                              borderRadius: 2,
                              bgcolor: '#f3f2f1',
                              '& .MuiLinearProgress-bar': { bgcolor: barColor, borderRadius: 2 },
                            }}
                          />
                        </Box>
                      );
                    })}
                  </Stack>
                )}
              </Box>
            </Stack>
          </Grid>

        </Grid>
      </Box>
    </Box>
  );
}
