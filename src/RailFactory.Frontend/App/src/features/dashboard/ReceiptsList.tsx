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
  Tooltip
} from '@mui/material';
import DescriptionIcon from '@mui/icons-material/Description';
import type { Receipt } from './types';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
};

/**
 * Renders a list of material receipts for the current tenant.
 * @param tenantCode - The active tenant identifier for data filtering.
 * @param refreshKey - Optional trigger to force re-fetch from the API.
 * @remarks
 * This component handles data fetching, loading states, and error handling.
 * It includes a specialized download function for the original NF-e XML.
 */
export function ReceiptsList({ tenantCode, refreshKey = 0 }: ReceiptsListProps) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  /**
   * Fetches and downloads the original XML content for a specific receipt.
   * @param receiptId - The unique ID of the receipt.
   * @param receiptNumber - The human-readable name for the downloaded file.
   * @remarks
   * Uses a Blob/ObjectURL pattern to trigger a browser download from the API stream.
   */
  const viewXml = async (receiptId: string, receiptNumber: string) => {
    try {
      const response = await fetch(`/api/supply-chain/receipts/${receiptId}/xml`, {
        headers: {
          'X-Tenant-Code': tenantCode
        },
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

  useEffect(() => {
    setLoading(true);
    setError(null);

    fetch('/api/supply-chain/receipts', {
      headers: {
        'X-Tenant-Code': tenantCode
      },
      credentials: 'include'
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Receipts request failed: ${response.status}`);
        }

        return response.json() as Promise<Receipt[]>;
      })
      .then(data => {
        setReceipts(data);
        setLoading(false);
      })
      .catch((requestError: Error) => {
        setError(requestError.message);
        setLoading(false);
      });
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
          {receipts.map(receipt => (
            <TableRow key={receipt.id} hover>
              <TableCell sx={{ fontWeight: 600 }}>{receipt.receiptNumber}</TableCell>
              <TableCell>
                <Typography variant="body2" sx={{ fontWeight: 500 }}>{receipt.documentNumber}</Typography>
                {receipt.accessKey && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontFamily: 'monospace' }}>
                    {receipt.accessKey}
                  </Typography>
                )}
              </TableCell>
              <TableCell>{receipt.status}</TableCell>
              <TableCell>
                {receipt.totalValue ? `R$ ${receipt.totalValue.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}` : '-'}
              </TableCell>
              <TableCell>{receipt.itemCount}</TableCell>
              <TableCell align="right">
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
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
