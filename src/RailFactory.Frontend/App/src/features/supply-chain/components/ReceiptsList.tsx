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
  CircularProgress,
  Alert,
  Paper,
  IconButton,
  Tooltip,
  Chip
} from '@mui/material';
import DescriptionIcon from '@mui/icons-material/Description';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import type { Receipt } from '../types';
import { ReceiptDetailsModal } from './ReceiptDetailsModal';
import { getStatusMapping } from '../../../shared/lib/utils/status-mapping';
import { TechnicalIdFormatter } from '../../../shared/lib/utils/formatters';
import { buildTenantHeaders, fetchJsonOrThrow } from '../../../shared/lib/http';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
  onStartConference?: (receiptId: string) => void;
};

/**
 * Renders a list of material receipts for the current tenant.
 * @param tenantCode - The active tenant identifier for data filtering.
 * @param refreshKey - Optional trigger to force re-fetch from the API.
 * @param onStartConference - Callback when user wants to enter conference view.
 * @remarks
 * This component handles data fetching, loading states, and error handling.
 * It includes a specialized download function for the original NF-e XML.
 */
export function ReceiptsList({ tenantCode, refreshKey = 0, onStartConference }: ReceiptsListProps) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);

  /**
   * Initiates the blind conference process for a receipt.
   */
  const startConference = async (receiptId: string) => {
    try {
      await fetchJsonOrThrow(
        `/api/supply-chain/receipts/${receiptId}/conference/start`,
        {
          method: 'POST',
          headers: buildTenantHeaders(tenantCode),
          credentials: 'include'
        },
        'Failed to start conference'
      );

      if (onStartConference) {
        onStartConference(receiptId);
      } else {
        fetchReceipts();
      }
    } catch (err) {
      console.error(err);
      alert('Error starting conference.');
    }
  };

  /**
   * Fetches and downloads the original XML content for a specific receipt.
   */
  const viewXml = async (receiptId: string, receiptNumber: string) => {
    try {
      const response = await fetch(`/api/supply-chain/receipts/${receiptId}/xml`, {
        headers: buildTenantHeaders(tenantCode),
        credentials: 'include'
      });

      if (!response.ok) {
        if (response.status === 404) {
          alert('XML not found for this receipt.');
          return;
        }
        throw new Error('Failed to fetch XML');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${receiptNumber}.xml`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error(err);
      alert('Error downloading XML.');
    }
  };

  const fetchReceipts = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await fetchJsonOrThrow<Receipt[]>(
        '/api/supply-chain/receipts',
        {
          headers: buildTenantHeaders(tenantCode),
          credentials: 'include'
        },
        'Receipts request failed'
      );
      setReceipts(data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchReceipts();
  }, [tenantCode, refreshKey]);


  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
        <CircularProgress size={32} />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ my: 2 }}>{error}</Alert>
    );
  }

  if (receipts.length === 0) {
    return (
      <Box sx={{ p: 4, textAlign: 'center' }}>
        <Typography color="text.secondary">No receipts yet.</Typography>
      </Box>
    );
  }

  return (
    <>
      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow sx={{ bgcolor: 'background.default' }}>
              <TableCell sx={{ fontWeight: 700 }}>Receipt</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Document / Access Key</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Total</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Items</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {receipts.map(receipt => {
              const status = getStatusMapping(receipt.status);
              return (
                <TableRow key={receipt.id} hover>
                  <TableCell sx={{ fontWeight: 600 }}>{receipt.receiptNumber}</TableCell>
                  <TableCell>
                    <Tooltip title="Click to copy document number">
                      <Typography 
                        variant="body2" 
                        sx={{ fontWeight: 500, cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                        onClick={() => TechnicalIdFormatter.copyToClipboard(receipt.documentNumber)}
                      >
                        {receipt.documentNumber}
                      </Typography>
                    </Tooltip>
                    {receipt.accessKey && (
                      <Tooltip title="Click to copy full access key">
                        <Typography 
                          variant="caption" 
                          color="text.secondary" 
                          sx={{ display: 'block', fontFamily: 'monospace', cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
                          onClick={() => receipt.accessKey && TechnicalIdFormatter.copyToClipboard(receipt.accessKey)}
                        >
                          {TechnicalIdFormatter.truncate(receipt.accessKey)}...
                        </Typography>
                      </Tooltip>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip 
                      size="small" 
                      label={status.label} 
                      color={status.color as any} 
                      variant="outlined"
                      sx={{ fontWeight: 700 }}
                    />
                  </TableCell>
                  <TableCell>
                    {receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}
                  </TableCell>
                  <TableCell>{receipt.itemCount}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="View Full Details">
                      <IconButton 
                        size="small" 
                        color="info"
                        onClick={() => setSelectedReceiptId(receipt.id)}
                      >
                        <InfoOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {receipt.status === 'Registered' && (
                      <Tooltip title="Start Conference">
                        <IconButton 
                          size="small" 
                          color="success"
                          onClick={() => startConference(receipt.id)}
                        >
                          <PlayArrowIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    {receipt.status === 'InConference' && (
                      <Tooltip title="Count Items">
                        <IconButton 
                          size="small" 
                          color="warning"
                          onClick={() => onStartConference && onStartConference(receipt.id)}
                        >
                          <CheckCircleIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Download original XML">
                      <IconButton 
                        size="small" 
                        color="primary"
                        onClick={() => viewXml(receipt.id, receipt.receiptNumber)}
                      >
                        <DescriptionIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <ReceiptDetailsModal 
        receiptId={selectedReceiptId} 
        tenantCode={tenantCode} 
        onClose={() => setSelectedReceiptId(null)} 
      />
    </>
  );
}
