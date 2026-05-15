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
  Grid,
  alpha,
  useTheme
} from '@mui/material';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { InlineError } from '../../../shared/components/common/InlineError';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow, toUiErrorMessage } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';
import { StatusChip } from '../../../shared/components/common/StatusChip';
import type { DisplayStatus } from '../../../shared/lib/utils/status-mapping';

type ReceiptDetailsModalProps = {
  receiptId: string | null;
  tenantCode: string;
  onClose: () => void;
};

type MaterialReceiptDetails = {
  id: string;
  receiptNumber: string;
  status: DisplayStatus;
  supplier?: {
    name: string;
    taxId: string;
  };
  issuedAt: string;
  audit: {
    createdAt: string;
    createdBy: string;
    conferenceStartedAt?: string;
    conferenceStartedBy?: string;
  };
  items: Array<{
    id: string;
    materialCode: string;
    productName: string;
    originalDescription?: string;
    expectedQuantity: number;
    countedQuantity?: number;
    unitOfMeasure: string;
    unitPrice?: number;
    lotNumber?: string;
    expirationDate?: string;
    imageUrl?: string;
  }>;
  timeline: Array<{
    status: DisplayStatus;
    occurredAt: string;
  }>;
};

/**
 * Modal displaying full details of a Material Receipt.
 * @param props - Component properties.
 * @remarks
 * Architectural Decision: Uses StatusChip for unified status rendering.
 * Localization: All operator-facing labels are in Portuguese (Brazil).
 */
export function ReceiptDetailsModal({ receiptId, tenantCode, onClose }: ReceiptDetailsModalProps) {
  const theme = useTheme();
  const [details, setDetails] = useState<MaterialReceiptDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!receiptId) return;

    const fetchDetails = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchJsonOrThrow<MaterialReceiptDetails>(
          `/api/supply-chain/receipts/${receiptId}`,
          {
            headers: buildTenantHeaders(tenantCode),
            credentials: 'include'
          },
          'Falha ao carregar detalhes do recebimento'
        );
        setDetails(data);
      } catch (err) {
        setError(toUiErrorMessage(err, 'Não foi possível carregar os detalhes do recebimento.'));
      } finally {
        setLoading(false);
      }
    };

    void fetchDetails();
  }, [receiptId, tenantCode]);

  return (
    <ResponsiveCenteredModal 
      open={!!receiptId} 
      title={`DETALHES DO RECEBIMENTO: ${details?.receiptNumber || '...'}`} 
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
          {/* Header Info */}
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 6 }}>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>FORNECEDOR</Typography>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>{details.supplier?.name || 'Desconhecido'}</Typography>
              <Typography variant="body2" color="text.secondary">CNPJ: {details.supplier?.taxId}</Typography>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }} sx={{ textAlign: { md: 'right' } }}>
              <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 800 }}>STATUS ATUAL</Typography>
              <Box sx={{ display: 'flex', justifyContent: { md: 'flex-end' }, mt: 0.5 }}>
                <StatusChip status={details.status} />
              </Box>
              <Typography variant="caption" sx={{ mt: 1, display: 'block' }}>
                Emissão: {formatRelativeDate(details.issuedAt)}
              </Typography>
            </Grid>
          </Grid>

          <Divider />

          {/* Timeline Section */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>LINHA DO TEMPO</Typography>
            <Box sx={{ position: 'relative', pl: 3, borderLeft: '2px solid', borderColor: 'divider' }}>
              {details.timeline.map((event, idx) => (
                <Box key={idx} sx={{ mb: 2, position: 'relative' }}>
                  <Box 
                    sx={{ 
                      position: 'absolute', 
                      left: -33, 
                      top: 4, 
                      width: 14, 
                      height: 14, 
                      borderRadius: '50%', 
                      bgcolor: (theme) => (theme.palette as any)[event.status.color]?.main || theme.palette.grey[400],
                      border: '3px solid white',
                      boxShadow: 1
                    }} 
                  />
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {event.status.label}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatRelativeDate(event.occurredAt)}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Box>

          <Divider />

          {/* Items Table */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 800, mb: 2 }}>CONFERÊNCIA DE ITENS</Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: 'background.default' }}>
                    <TableCell sx={{ fontWeight: 800 }}>MATERIAL</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>ESPERADO</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>CONTADO</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800 }}>DIF</TableCell>
                    <TableCell sx={{ fontWeight: 800 }}>LOTE / VALIDADE</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {details.items.map((item) => {
                    const diff = item.countedQuantity !== undefined ? item.countedQuantity - item.expectedQuantity : null;
                    return (
                      <TableRow key={item.id} hover>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                            <MaterialAvatar 
                              materialCode={item.materialCode} 
                              imageUrl={item.imageUrl}
                              size={28} 
                            />
                            <Box>
                              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {item.productName}
                              </Typography>
                              <Typography variant="caption" color="text.secondary" component="span" sx={{ display: 'block', fontWeight: 600, fontFamily: 'monospace' }}>
                                {item.materialCode}
                              </Typography>
                            </Box>
                          </Box>
                        </TableCell>
                        <TableCell align="right">{item.expectedQuantity} {item.unitOfMeasure}</TableCell>
                        <TableCell align="right">{item.countedQuantity ?? '-'} {item.unitOfMeasure}</TableCell>
                        <TableCell align="right">
                          {diff !== null && (
                            <Typography 
                              variant="body2" 
                              sx={{ 
                                fontWeight: 700, 
                                color: diff === 0 ? 'success.main' : 'error.main' 
                              }}
                            >
                              {diff > 0 ? `+${diff}` : diff}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell>
                          <Typography variant="caption" sx={{ display: 'block', fontWeight: 600 }}>{item.lotNumber || '-'}</Typography>
                          <Typography variant="caption" color="text.secondary">{item.expirationDate ? formatRelativeDate(item.expirationDate, false) : '-'}</Typography>
                          <Typography variant="caption" color="primary.main" sx={{ display: 'block' }}>
                            {item.unitPrice !== undefined && item.unitPrice !== null
                              ? `R$ ${item.unitPrice.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`
                              : '-'}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>

          <Divider />

          {/* Audit Trail */}
          <Box sx={{ bgcolor: alpha(theme.palette.primary.main, 0.03), p: 2, borderRadius: 1 }}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800, display: 'block', mb: 1 }}>
              TRILHA DE AUDITORIA
            </Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Typography variant="caption" sx={{ display: 'block' }}>Criação: <strong>{formatRelativeDate(details.audit.createdAt)}</strong></Typography>
                <Typography variant="caption" sx={{ display: 'block' }}>Por: <strong>{details.audit.createdBy}</strong></Typography>
              </Grid>
              {details.audit.conferenceStartedAt && (
                <Grid size={{ xs: 12, sm: 6 }}>
                  <Typography variant="caption" sx={{ display: 'block' }}>Início Conf.: <strong>{formatRelativeDate(details.audit.conferenceStartedAt)}</strong></Typography>
                  <Typography variant="caption" sx={{ display: 'block' }}>Por: <strong>{details.audit.conferenceStartedBy}</strong></Typography>
                </Grid>
              )}
            </Grid>
            <Typography variant="caption" sx={{ mt: 1, display: 'block', opacity: 0.5, fontFamily: 'monospace' }}>
              System ID: {details.id}
            </Typography>
          </Box>
        </Stack>
      )}
    </ResponsiveCenteredModal>
  );
}
