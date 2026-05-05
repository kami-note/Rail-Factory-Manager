import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography
} from '@mui/material';
import type { PendingBalance } from './types';

type InventoryStocksPageProps = {
  tenantCode: string;
};

export function InventoryStocksPage({ tenantCode }: InventoryStocksPageProps) {
  const [balances, setBalances] = useState<PendingBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);

    fetch('/api/inventory/balances/pending', {
      headers: {
        'X-Tenant-Code': tenantCode
      },
      credentials: 'include'
    })
      .then(async response => {
        if (!response.ok) {
          throw new Error(`Inventory request failed: ${response.status}`);
        }

        return response.json() as Promise<PendingBalance[]>;
      })
      .then(data => {
        setBalances(data);
        setLoading(false);
      })
      .catch((requestError: Error) => {
        setError(requestError.message);
        setLoading(false);
      });
  }, [tenantCode]);

  return (
    <Box sx={{ p: { xs: 2, md: 4 } }}>
      <Box sx={{ mb: 3 }}>
        <Typography variant="h1" sx={{ fontWeight: 900, mb: 0.5 }}>
          INVENTORY
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
          PENDING STOCK BALANCES
        </Typography>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 6 }}>
          <CircularProgress size={32} />
        </Box>
      ) : null}

      {error ? (
        <Alert severity="error" sx={{ my: 2 }}>
          {error}
        </Alert>
      ) : null}

      {!loading && !error && balances.length === 0 ? (
        <Box sx={{ p: 4, textAlign: 'center', bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }}>
          <Typography color="text.secondary">No pending balances.</Typography>
        </Box>
      ) : null}

      {!loading && !error && balances.length > 0 ? (
        <TableContainer component={Paper} variant="outlined">
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'background.default' }}>
                <TableCell sx={{ fontWeight: 700 }}>Material</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Quantity</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>UoM</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Source Reference</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Created At</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {balances.map(balance => (
                <TableRow key={balance.id} hover>
                  <TableCell sx={{ fontWeight: 600 }}>{balance.materialCode}</TableCell>
                  <TableCell>{balance.quantity}</TableCell>
                  <TableCell>{balance.unitOfMeasure}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={String(balance.status).toUpperCase()}
                      color={String(balance.status).toLowerCase() === 'pending' ? 'warning' : 'default'}
                    />
                  </TableCell>
                  <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{balance.sourceReference}</TableCell>
                  <TableCell>{new Date(balance.createdAt).toLocaleString()}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}
    </Box>
  );
}
