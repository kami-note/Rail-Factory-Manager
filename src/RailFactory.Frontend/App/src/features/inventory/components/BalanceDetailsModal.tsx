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
  Alert,
  Button
} from '@mui/material';
import { Camera } from 'lucide-react';
import { ResponsiveCenteredModal } from '../../../shared/components/ResponsiveCenteredModal';
import { formatRelativeDate, TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { getStatusMapping } from '../../../shared/lib/utils/status-mapping';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';
import { MaterialAvatar } from '../../../shared/components/common/MaterialAvatar';

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
    category: string;
    status: string;
    imageUrl?: string;
    ncm?: string;
    gtin?: string;
  };
  unitOfMeasure: string;
  status: string;
  quantities: {
    totalPhysical: number;
    available: number;
    blocked: number;
    quarantine: number;
  };
  traceability: {
    lotNumber?: string;
    expirationDate?: string;
    sourceType: string;
    sourceReference: string;
    supplierName?: string;
  };
  ledger: Array<{
    occurredAt: string;
    quantityChange: number;
    newStatus: string;
    reason: string;
    user: string;
  }>;
};

/**
 * Modal displaying full details of an Inventory Balance, including technical metadata and image management.
 */
export function BalanceDetailsModal({ balanceId, tenantCode, onClose }: BalanceDetailsModalProps) {
  const [details, setDetails] = useState<InventoryBalanceDetails | null>(null);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchDetails = async () => {
    if (!balanceId) return;
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

  useEffect(() => {
    void fetchDetails();
  }, [balanceId, tenantCode]);

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !details) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);

    try {
      await fetchJsonOrThrow(
        `/api/materials/${details.materialCode}/image`,
        {
          method: 'POST',
          headers: {
            'X-Tenant-Code': tenantCode
          },
          body: formData,
          credentials: 'include'
        },
        'Image upload failed'
      );
      await fetchDetails();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const balanceStatus = details ? getStatusMapping(details.status) : null;

  return (
    <ResponsiveCenteredModal 
      open={!!balanceId} 
      title={`BALANCE DETAILS: ${details?.material.officialName || '...'}`} 
      onClose={onClose}
    >
      {loading && !details && (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <Alert severity="error">{error}</Alert>
      )}

      {details && (
        <Stack spacing={4}>
          {/* Material Info Section with Image Upload */}
          <Box sx={{ display: 'flex', gap: 3, alignItems: 'flex-start' }}>
            <Box sx={{ position: 'relative' }}>
              <MaterialAvatar materialCode={details.materialCode} size={100} />
              {details.material.imageUrl && (
                <Box 
                  component="img" 
                  src={details.material.imageUrl} 
                  sx={{ position: 'absolute', top: 0, left: 0, width: 100, height: 100, borderRadius: 1, objectFit: 'cover', border: '1px solid #ddd' }} 
                />
              )}
              <Button
                component="label"
                variant="contained"
                size="small"
                disabled={uploading}
                sx={{ 
                  position: 'absolute', bottom: -10, left: '50%', transform: 'translateX(-50%)', 
                  minWidth: 0, p: 0.5, borderRadius: '50%', width: 32, height: 32 
                }}
              >
                {uploading ? <CircularProgress size={16} color="inherit" /> : <Camera size={16} />}
                <input type="file" hidden accept="image/*" onChange={handleImageUpload} />
              </Button>
            </Box>

            <Box sx={{ flexGrow: 1 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 800, color: 'primary.main' }}>
                    {details.material.officialName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, display: 'block' }}>
                    SKU: {details.materialCode} | CAT: {details.material.category} | STATUS: {details.material.status}
                  </Typography>
                </Box>
                {balanceStatus && (
                  <Chip 
                    label={balanceStatus.label} 
                    color={balanceStatus.color as any} 
                    size="small" 
                    sx={{ fontWeight: 700 }} 
                  />
                )}
              </Box>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600, display: 'block' }}>
                NCM: {details.material.ncm || '---'} | GTIN: {details.material.gtin || '---'}
              </Typography>
              <Typography variant="body2" sx={{ mt: 1, color: 'text.secondary' }}>
                {details.material.description}
              </Typography>
            </Box>
          </Box>

          <Divider />

          {/* Magic Numbers Section */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 2 }}>QUANTITY BREAKDOWN</Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 6, md: 3 }}>
                <Paper variant="outlined" sx={{ p: 2, textAlign: 'center', bgcolor: 'grey.50' }}>
                  <Typography variant="overline" color="text.secondary">Total Physical</Typography>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.totalPhysical}</Typography>
                  <Typography variant="caption">{details.unitOfMeasure}</Typography>
                </Paper>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Paper variant="outlined" sx={{ p: 2, textAlign: 'center', borderLeft: '4px solid', borderLeftColor: 'success.main' }}>
                  <Typography variant="overline" color="success.main">Available</Typography>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.available}</Typography>
                  <Typography variant="caption">{details.unitOfMeasure}</Typography>
                </Paper>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Paper variant="outlined" sx={{ p: 2, textAlign: 'center', borderLeft: '4px solid', borderLeftColor: 'error.main' }}>
                  <Typography variant="overline" color="error.main">Blocked</Typography>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.blocked}</Typography>
                  <Typography variant="caption">{details.unitOfMeasure}</Typography>
                </Paper>
              </Grid>
              <Grid size={{ xs: 6, md: 3 }}>
                <Paper variant="outlined" sx={{ p: 2, textAlign: 'center', borderLeft: '4px solid', borderLeftColor: 'warning.main' }}>
                  <Typography variant="overline" color="warning.main">Quarantine</Typography>
                  <Typography variant="h5" sx={{ fontWeight: 800 }}>{details.quantities.quarantine}</Typography>
                  <Typography variant="caption">{details.unitOfMeasure}</Typography>
                </Paper>
              </Grid>
            </Grid>
          </Box>

          <Divider />

          {/* Traceability Section */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 2 }}>TRACEABILITY & ORIGIN</Typography>
            <Grid container spacing={3}>
              <Grid size={{ xs: 12, md: 6 }}>
                <Stack spacing={1}>
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>Lot Number</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{details.traceability.lotNumber || 'N/A'}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>Expiration Date</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {details.traceability.expirationDate ? formatRelativeDate(details.traceability.expirationDate, false) : 'N/A'}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>Supplier</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{details.traceability.supplierName || 'N/A'}</Typography>
                  </Box>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <Stack spacing={1}>
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>Source Type</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{details.traceability.sourceType}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>Source Reference</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600, fontFamily: 'monospace' }}>
                      {details.traceability.sourceReference}
                    </Typography>
                  </Box>
                </Stack>
              </Grid>
            </Grid>
          </Box>

          <Divider />

          {/* Ledger Section */}
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 2 }}>MOVEMENT HISTORY (LEDGER)</Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow sx={{ bgcolor: 'grey.50' }}>
                    <TableCell sx={{ fontWeight: 700 }}>Date</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Operation</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Delta</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>User</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {details.ledger.map((entry, index) => (
                    <TableRow key={index} hover>
                      <TableCell>{formatRelativeDate(entry.occurredAt)}</TableCell>
                      <TableCell sx={{ fontWeight: 500 }}>{entry.reason}</TableCell>
                      <TableCell sx={{ color: entry.quantityChange >= 0 ? 'success.main' : 'error.main', fontWeight: 700 }}>
                        {entry.quantityChange >= 0 ? '+' : ''}{entry.quantityChange}
                      </TableCell>
                      <TableCell>
                        <Chip size="small" label={entry.newStatus} variant="outlined" sx={{ fontWeight: 700, height: 20, fontSize: '0.65rem' }} />
                      </TableCell>
                      <TableCell>{entry.user}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            
            <Typography variant="caption" sx={{ mt: 1, display: 'block', opacity: 0.5, fontFamily: 'monospace' }}>
              System ID: {details.id}
            </Typography>
          </Box>
        </Stack>
      )}
    </ResponsiveCenteredModal>
  );
}
