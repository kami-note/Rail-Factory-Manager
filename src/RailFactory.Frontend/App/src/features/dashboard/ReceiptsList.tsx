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
  Paper
} from '@mui/material';
import type { Receipt } from './types';

type ReceiptsListProps = {
  tenantCode: string;
  refreshKey?: number;
};

export function ReceiptsList({ tenantCode, refreshKey = 0 }: ReceiptsListProps) {
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
            <TableCell sx={{ fontWeight: 700 }}>Document</TableCell>
            <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
            <TableCell sx={{ fontWeight: 700 }}>Items</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {receipts.map(receipt => (
            <TableRow key={receipt.id} hover>
              <TableCell sx={{ fontWeight: 600 }}>{receipt.receiptNumber}</TableCell>
              <TableCell>{receipt.documentNumber}</TableCell>
              <TableCell>{receipt.status}</TableCell>
              <TableCell>{receipt.itemCount}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
