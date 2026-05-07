import React, { useEffect, useState } from 'react';
import { 
  Box, 
  Typography, 
  Grid, 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Paper, 
  Chip,
  CircularProgress,
  Divider,
  Stack,
  Alert
} from '@mui/material';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { getStatusMapping } from '../../../shared/lib/utils/status-mapping';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';

type ReceiptDetailsModalProps = {
  receiptId: string | null;
  tenantCode: string;
  onClose: () => void;
};

type MaterialReceiptDetails = {
  id: string;
  receiptNumber: string;
  status: string;
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
  }>;
  timeline: Array<{
    status: string;
    occurredAt: string;
  }>;
};

/**
 * Modal displaying full details of a Material Receipt.
 * @param receiptId - The ID of the receipt to fetch and display.
 * @param tenantCode - Active tenant for API headers.
 * @param onClose - Callback to close the modal.
 */
export function ReceiptDetailsModal({ receiptId, tenantCode, onClose }: ReceiptDetailsModalProps) {
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
          'Failed to load receipt details'
        );
        setDetails(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    void fetchDetails();
  }, [receiptId, tenantCode]);

  const status = details ? getStatusMapping(details.status) : null;

  return (
    <ResponsiveCenteredModal 
      open={!!receiptId} 
      title={`RECEIPT DETAILS: ${details?.receiptNumber || '...'}`} 
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
          {/* Header Info */}
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 6 }}>
              <Typography variant="overline" color="text.secondary">Supplier</Typography>
              <Typography variant="h6" sx={{ fontWeight: 700 }}>{details.supplier?.name || 'Unknown'}</Typography>
              <Typography variant="body2" color="text.secondary">Tax ID: {details.supplier?.taxId}</Typography>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }} sx={{ textAlign: { md: 'right' } }}>
              <Typography variant="overline" color="text.secondary">Status</Typography>
              <Box sx={{ display: 'flex', justifyContent: { md: 'flex-end' }, mt: 0.5 }}>
                <Chip 
                  label={status?.label} 
                  color={status?.color as any} 
                  sx={{ fontWeight: 700, px: 1 }} 
                />
              </Box>
              <Typography variant="caption" sx={{ mt: 1, display: 'block' }}>
                Issued at: {formatRelativeDate(details.issuedAt)}
              </Typography>
            </Grid>
          </Grid>

          <Divider />

          {/* Timeline Section */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 2 }}>TIMELINE</Typography>
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
                      bgcolor: getStatusMapping(event.status).color + '.main',
                      border: '3px solid white',
                      boxShadow: 1
                    }} 
                  />
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {getStatusMapping(event.status).label}
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
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 2 }}>ITEMS COMPARISON</Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: 'background.default' }}>
                    <TableCell sx={{ fontWeight: 700 }}>Material</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Expected</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Counted</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Diff</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Lot / Expiry</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {details.items.map((item) => {
                    const diff = item.countedQuantity !== undefined ? item.countedQuantity - item.expectedQuantity : null;
                    return (
                      <TableRow key={item.id}>
                        <TableCell>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                            <MaterialAvatar materialCode={item.materialCode} size={28} />
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                              {item.productName}
                            </Typography>
                            {item.originalDescription && item.originalDescription !== item.productName && (
                              <Typography variant="caption" color="text.secondary">
                                {item.originalDescription}
                              </Typography>
                            )}
                            <Typography variant="caption" color="text.secondary">
                              {item.materialCode}
                            </Typography>
                          </Box>
                        </TableCell>
                        <TableCell>{item.expectedQuantity} {item.unitOfMeasure}</TableCell>
                        <TableCell>{item.countedQuantity ?? '-'} {item.unitOfMeasure}</TableCell>
                        <TableCell>
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
                          <Typography variant="caption" sx={{ display: 'block' }}>{item.lotNumber || '-'}</Typography>
                          <Typography variant="caption" color="text.secondary">{item.expirationDate ? formatRelativeDate(item.expirationDate, false) : '-'}</Typography>
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
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
          <Box sx={{ bgcolor: 'grey.50', p: 2, borderRadius: 1 }}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, display: 'block', mb: 1 }}>
              AUDIT TRAIL
            </Typography>
            <Grid container spacing={2}>
              <Grid size={6}>
                <Typography variant="caption" sx={{ display: 'block' }}>Created: {formatRelativeDate(details.audit.createdAt)}</Typography>
                <Typography variant="caption" sx={{ display: 'block' }}>By: {details.audit.createdBy}</Typography>
              </Grid>
              {details.audit.conferenceStartedAt && (
                <Grid size={6}>
                  <Typography variant="caption" sx={{ display: 'block' }}>Conference: {formatRelativeDate(details.audit.conferenceStartedAt)}</Typography>
                  <Typography variant="caption" sx={{ display: 'block' }}>By: {details.audit.conferenceStartedBy}</Typography>
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
