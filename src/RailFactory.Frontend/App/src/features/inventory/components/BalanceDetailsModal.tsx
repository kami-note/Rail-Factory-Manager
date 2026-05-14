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
  Alert,
  alpha,
  useTheme,
  Grid
} from '@mui/material';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
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
 * Modal displaying full details and ledger for an Inventory Balance.
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
          'Failed to load balance details'
        );
        setDetails(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    void fetchDetails();
  }, [balanceId, tenantCode]);

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
        <Alert severity="error">{error}</Alert>
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
                </Box>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }} sx={{ textAlign: { md: 'right' } }}>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>STATUS ATUAL</Typography>
              <Box sx={{ display: 'flex', justifyContent: { md: 'flex-end' }, mt: 0.5 }}>
                <StatusChip status={details.status} />
              </Box>
            </Grid>
          </Grid>

          <Divider />

          {/* Stock Metrics */}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr 1fr' }, gap: 3 }}>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>QUANTIDADE</Typography>
              <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.totalPhysical} {details.unitOfMeasure}</Typography>
            </Box>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>LOTE / VALIDADE</Typography>
              <Typography variant="body1" sx={{ fontWeight: 700 }}>{details.traceability.lotNumber || 'N/A'}</Typography>
              {details.traceability.expirationDate && (
                <Typography variant="caption" color="text.secondary">Vencimento: {formatRelativeDate(details.traceability.expirationDate, false)}</Typography>
              )}
            </Box>
            <Box>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>ORIGEM</Typography>
              <Box sx={{ mt: 0.5 }}>
                <StatusChip status={details.traceability.sourceType} label={details.traceability.supplierName || details.traceability.sourceType.label} />
              </Box>
              <Typography variant="caption" color="text.disabled" sx={{ display: 'block', mt: 0.5, fontFamily: 'monospace' }}>
                Ref: {TechnicalIdFormatter.truncate(details.traceability.sourceReference)}
              </Typography>
            </Box>
          </Box>

          <Divider />

          {/* Ledger / History */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>HISTÓRICO DE MOVIMENTAÇÕES (LEDGER)</Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: 'background.default' }}>
                    <TableCell sx={{ fontWeight: 800 }}>DATA</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>VARIÂÇÃO</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>NOVO STATUS</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>MOTIVO</TableCell>
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
                        <Typography variant="caption" sx={{ fontWeight: 600 }}>{entry.reason}</Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption">{entry.user}</Typography>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
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
